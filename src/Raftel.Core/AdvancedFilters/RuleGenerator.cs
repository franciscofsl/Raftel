using System.Linq.Expressions;

namespace Raftel.Core.AdvancedFilters;

public class RuleGenerator<TModel> : IFilterRuleBuilder<TModel>
{
    private readonly List<Rule> _rules;
    private readonly List<Rule> _andNestedRules = new();
    private readonly List<Rule> _orNestedRules = new();
    private readonly Condition _currentCondition;

    public RuleGenerator(List<Rule> rules, Condition condition)
    {
        _rules = rules;
        _currentCondition = condition;
    }

    public IFilterRuleBuilder<TModel> StartsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.StartsWith, expression, FieldType.String, value);
    }

    public IFilterRuleBuilder<TModel> NotStartsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.NotStartsWith, expression, FieldType.String, value);
    }

    public IFilterRuleBuilder<TModel> EndsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.EndsWith, expression, FieldType.String, value);
    }

    public IFilterRuleBuilder<TModel> NotEndsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.NotEndsWith, expression, FieldType.String, value);
    }

    public IFilterRuleBuilder<TModel> Contains(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.Contains, expression, FieldType.String, value);
    }

    public IFilterRuleBuilder<TModel> NotContains(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.NotContains, expression, FieldType.String, value);
    }

    public IFilterRuleBuilder<TModel> Equal(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.Equal, expression, FieldType.String, value);
    }

    public IFilterRuleBuilder<TModel> NotEqual(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.NotEqual, expression, FieldType.String, value);
    }

    public IFilterRuleBuilder<TModel> In(Expression<Func<TModel, object>> expression, string[] values)
    {
        return AddRule(Operator.In, expression, FieldType.String, values);
    }

    public IFilterRuleBuilder<TModel> NotIn(Expression<Func<TModel, object>> expression, string[] values)
    {
        return AddRule(Operator.NotIn, expression, FieldType.String, values);
    }

    public IFilterRuleBuilder<TModel> Empty(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.Empty, expression, FieldType.String, null);
    }

    public IFilterRuleBuilder<TModel> NotEmpty(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.NotEmpty, expression, FieldType.String, null);
    }

    public IFilterRuleBuilder<TModel> Null(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.Null, expression, FieldType.String, null);
    }

    public IFilterRuleBuilder<TModel> NotNull(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.NotNull, expression, FieldType.String, null);
    }

    public IFilterRuleBuilder<TModel> And(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression)
    {
        var nestedRules = new List<Rule>();
        var nestedBuilder = new RuleGenerator<TModel>(nestedRules, Condition.And);
        filterExpression.Compile().Invoke(nestedBuilder);
        _andNestedRules.AddRange(nestedRules);
        return this;
    }


    public IFilterRuleBuilder<TModel> Or(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression)
    {
        var nestedRules = new List<Rule>();
        var nestedBuilder = new RuleGenerator<TModel>(nestedRules, Condition.Or);
        filterExpression.Compile().Invoke(nestedBuilder);

        _orNestedRules.AddRange(nestedRules);
        return this;
    }

    public List<Rule> GetRulesWithCondition(Condition condition)
    {
        return _rules.Select(rule => rule with { Condition = condition }).ToList();
    }

    internal Expression CreateExpression(ParameterExpression parameter, Rule rule)
    {
        if (rule.Nested != null && rule.Nested.Any())
        {
            Expression nestedExpression = null;
            foreach (var nestedRule in rule.Nested)
            {
                var currentExpression = CreateExpression(parameter, nestedRule);

                if (nestedExpression == null)
                {
                    nestedExpression = currentExpression;
                }
                else
                {
                    nestedExpression = CombineExpressions(nestedExpression, currentExpression, rule.Condition);
                }
            }

            return nestedExpression;
        }

        var member = Expression.Property(parameter, rule.Field);
        var constantValue = Expression.Constant(rule.Value);
        var isNotNull = Expression.NotEqual(member, Expression.Constant(null));

        return rule.Operator switch
        {
            Operator.StartsWith => Expression.AndAlso(
                isNotNull,
                Expression.Call(member, typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) }),
                    constantValue)
            ),

            Operator.NotStartsWith => Expression.AndAlso(
                isNotNull,
                Expression.Not(Expression.Call(member,
                    typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) }), constantValue))
            ),

            Operator.EndsWith => Expression.AndAlso(
                isNotNull,
                Expression.Call(member, typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) }),
                    constantValue)
            ),

            Operator.NotEndsWith => Expression.AndAlso(
                isNotNull,
                Expression.Not(Expression.Call(member,
                    typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) }), constantValue))
            ),

            Operator.Contains => Expression.AndAlso(
                isNotNull,
                Expression.Call(member, typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) }),
                    constantValue)
            ),

            Operator.NotContains => Expression.AndAlso(
                isNotNull,
                Expression.Not(Expression.Call(member,
                    typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) }), constantValue))
            ),

            Operator.Equal => Expression.Equal(member, constantValue),

            Operator.NotEqual => Expression.Not(Expression.Equal(member, constantValue)),

            Operator.In => Expression.AndAlso(
                isNotNull,
                Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new[] { typeof(string) },
                    Expression.Constant(rule.Value), member)
            ),

            Operator.NotIn => Expression.AndAlso(
                isNotNull,
                Expression.Not(Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains),
                    new[] { typeof(string) }, Expression.Constant(rule.Value), member))
            ),

            Operator.Empty => Expression.AndAlso(
                isNotNull,
                Expression.Equal(member, Expression.Constant(string.Empty))
            ),

            Operator.NotEmpty => Expression.AndAlso(
                isNotNull,
                Expression.NotEqual(member, Expression.Constant(string.Empty))
            ),

            Operator.Null => Expression.Equal(member, Expression.Constant(null)),

            Operator.NotNull => Expression.NotEqual(member, Expression.Constant(null)),

            _ => throw new NotImplementedException($"Operator {rule.Operator} is not implemented.")
        };
    }

    public Expression CombineExpressions(Expression left, Expression right, Condition condition)
    {
        return condition == Condition.And
            ? Expression.AndAlso(left, right)
            : Expression.OrElse(left, right);
    }

    private IFilterRuleBuilder<TModel> AddRule(Operator operatorType, Expression<Func<TModel, object>> expression,
        FieldType type, object value)
    {
        var body = expression.Body as MemberExpression;
        var propertyName = body.Member.Name;
        _rules.Add(new Rule(operatorType, propertyName, type, value, _currentCondition));
        return this;
    }
}