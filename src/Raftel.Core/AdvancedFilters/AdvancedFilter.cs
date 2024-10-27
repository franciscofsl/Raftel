using System.Linq.Expressions;

namespace Raftel.Core.AdvancedFilters;

public class AdvancedFilter
{
    private AdvancedFilter()
    {
    }

    public static AdvancedFilter<TModel> ForModel<TModel>(Condition condition = Condition.And)
    {
        return new AdvancedFilter<TModel>(condition);
    }
}

public class AdvancedFilter<TModel>(Condition Condition = Condition.And)
{
    private List<Rule> _rules = new();

    public AdvancedFilter<TModel> StartsWith(Expression<Func<TModel, object>> expression, string value,
        Condition condition = Condition.And)
    {
        return AddRule(Operator.StartsWith, expression, FieldType.String, value, condition);
    }

    public AdvancedFilter<TModel> NotStartsWith(Expression<Func<TModel, object>> expression, string value,
        Condition condition = Condition.And)
    {
        return AddRule(Operator.NotStartsWith, expression, FieldType.String, value, condition);
    }

    public AdvancedFilter<TModel> EndsWith(Expression<Func<TModel, object>> expression, string value,
        Condition condition = Condition.And)
    {
        return AddRule(Operator.EndsWith, expression, FieldType.String, value, condition);
    }

    public AdvancedFilter<TModel> NotEndsWith(Expression<Func<TModel, object>> expression, string value,
        Condition condition = Condition.And)
    {
        return AddRule(Operator.NotEndsWith, expression, FieldType.String, value, condition);
    }

    public AdvancedFilter<TModel> Contains(Expression<Func<TModel, object>> expression, string value,
        Condition condition = Condition.And)
    {
        return AddRule(Operator.Contains, expression, FieldType.String, value, condition);
    }

    public AdvancedFilter<TModel> NotContains(Expression<Func<TModel, object>> expression, string value,
        Condition condition = Condition.And)
    {
        return AddRule(Operator.NotContains, expression, FieldType.String, value, condition);
    }

    public AdvancedFilter<TModel> Equal(Expression<Func<TModel, object>> expression, string value,
        Condition condition = Condition.And)
    {
        return AddRule(Operator.Equal, expression, FieldType.String, value, condition);
    }

    public AdvancedFilter<TModel> NotEqual(Expression<Func<TModel, object>> expression, string value,
        Condition condition = Condition.And)
    {
        return AddRule(Operator.NotEqual, expression, FieldType.String, value, condition);
    }

    public Func<TModel, bool> Build()
    {
        var parameter = Expression.Parameter(typeof(TModel), "model");
        Expression finalExpression = null;

        foreach (var rule in _rules)
        {
            var currentExpression = CreateExpression(parameter, rule);

            if (finalExpression == null)
            {
                finalExpression = currentExpression;
            }
            else
            {
                finalExpression = CombineExpressions(finalExpression, currentExpression, rule.Condition);
            }
        }

        if (finalExpression == null)
        {
            return _ => false;
        }

        var lambda = Expression.Lambda<Func<TModel, bool>>(finalExpression, parameter);
        return lambda.Compile();
    }

    private AdvancedFilter<TModel> AddRule(Operator operatorType, Expression<Func<TModel, object>> expression,
        FieldType type, object value,
        Condition condition = Condition.And)
    {
        var body = expression.Body as MemberExpression;
        var propertyName = body.Member.Name;
        _rules.Add(new Rule(operatorType, propertyName, type, value, condition));
        return this;
    }

    private AdvancedFilter<TModel> AddRule(Operator operatorType, string field, FieldType type, object value,
        Condition condition = Condition.And)
    {
        _rules.Add(new Rule(operatorType, field, type, value, condition));
        return this;
    }

    private Expression CreateExpression(ParameterExpression parameter, Rule rule)
    {
        var member = Expression.Property(parameter, rule.Field);
        var constantValue = Expression.Constant(rule.Value.ToString());

        return rule.Operator switch
        {
            Operator.StartsWith => Expression.Call(member,
                typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) }), constantValue),

            Operator.NotStartsWith => Expression.Not(Expression.Call(member,
                typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) }), constantValue)),

            Operator.EndsWith => Expression.Call(member,
                typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) }), constantValue),

            Operator.NotEndsWith => Expression.Not(Expression.Call(member,
                typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) }), constantValue)),

            Operator.Contains => Expression.Call(member,
                typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) }), constantValue),
            
            Operator.NotContains => Expression.Not(Expression.Call(member,
                typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) }), constantValue)),

            Operator.Equal => Expression.Call(member,
                typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string) }), constantValue),

            Operator.NotEqual => Expression.Not(Expression.Call(member,
                typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string) }), constantValue)),

            _ => throw new NotImplementedException($"Operator {rule.Operator} is not implemented.")
        };
    }

    private Expression CombineExpressions(Expression left, Expression right, Condition condition)
    {
        return condition == Condition.And
            ? Expression.AndAlso(left, right)
            : Expression.OrElse(left, right);
    }
}