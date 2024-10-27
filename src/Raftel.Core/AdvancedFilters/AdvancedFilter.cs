using System.Linq.Expressions;
using Raftel.Core.AdvancedFilters;

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

public class AdvancedFilter<TModel>
{
    private List<Rule> _rules = new();
    private Condition _currentCondition;

    public AdvancedFilter(Condition condition = Condition.And)
    {
        _currentCondition = condition;
    }

    public AdvancedFilter<TModel> StartsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.StartsWith, expression, FieldType.String, value);
    }

    public AdvancedFilter<TModel> NotStartsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.NotStartsWith, expression, FieldType.String, value);
    }

    public AdvancedFilter<TModel> EndsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.EndsWith, expression, FieldType.String, value);
    }

    public AdvancedFilter<TModel> NotEndsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.NotEndsWith, expression, FieldType.String, value);
    }

    public AdvancedFilter<TModel> Contains(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.Contains, expression, FieldType.String, value);
    }

    public AdvancedFilter<TModel> NotContains(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.NotContains, expression, FieldType.String, value);
    }

    public AdvancedFilter<TModel> Equal(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.Equal, expression, FieldType.String, value);
    }

    public AdvancedFilter<TModel> NotEqual(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.NotEqual, expression, FieldType.String, value);
    }

    public AdvancedFilter<TModel> In(Expression<Func<TModel, object>> expression, string[] values)
    {
        return AddRule(Operator.In, expression, FieldType.String, values);
    }

    public AdvancedFilter<TModel> NotIn(Expression<Func<TModel, object>> expression, string[] values)
    {
        return AddRule(Operator.NotIn, expression, FieldType.String, values);
    }

    public AdvancedFilter<TModel> Empty(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.Empty, expression, FieldType.String, null);
    }

    public AdvancedFilter<TModel> NotEmpty(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.NotEmpty, expression, FieldType.String, null);
    }

    public AdvancedFilter<TModel> Null(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.Null, expression, FieldType.String, null);
    }

    public AdvancedFilter<TModel> NotNull(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.NotNull, expression, FieldType.String, null);
    }

    public AdvancedFilter<TModel> And(Expression<Func<AdvancedFilter<TModel>, AdvancedFilter<TModel>>> filterExpression)
    {
        var subFilter = filterExpression.Compile()(this);

        var rules = subFilter._rules
            .Select(_ => _ with
            {
                Condition = Condition.And
            })
            .ToList();
        _rules.AddRange(rules);

        return this;
    }

    public AdvancedFilter<TModel> Or(Expression<Func<AdvancedFilter<TModel>, AdvancedFilter<TModel>>> filterExpression)
    {
        var subFilter = filterExpression.Compile()(this);
         
        var rules = subFilter._rules
            .Select(_ => _ with
            {
                Condition = Condition.Or
            })
            .ToList();
        _rules.AddRange(rules);

        return this;
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
        FieldType type, object value)
    {
        var body = expression.Body as MemberExpression;
        var propertyName = body.Member.Name;
        _rules.Add(new Rule(operatorType, propertyName, type, value, _currentCondition));
        return this;
    }

    private Expression CreateExpression(ParameterExpression parameter, Rule rule)
    {
        var member = Expression.Property(parameter, rule.Field);
        var constantValue = Expression.Constant(rule.Value?.ToString());
        var constantEmptyString = Expression.Constant(string.Empty);

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

            Operator.Equal => Expression.Equal(member, constantValue),

            Operator.NotEqual => Expression.Not(Expression.Equal(member, constantValue)),

            Operator.In => Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains),
                new[] { typeof(string) },
                Expression.Constant(rule.Value), member),

            Operator.NotIn => Expression.Not(Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains),
                new[] { typeof(string) }, Expression.Constant(rule.Value), member)),

            Operator.Empty => Expression.Equal(member, constantEmptyString),

            Operator.NotEmpty => Expression.NotEqual(member, constantEmptyString),

            Operator.Null => Expression.Equal(member, Expression.Constant(null)),

            Operator.NotNull => Expression.NotEqual(member, Expression.Constant(null)),

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