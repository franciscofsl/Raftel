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
        var body = expression.Body as MemberExpression;
        var propertyName = body.Member.Name;

        return AddRule(Operator.StartsWith, propertyName, FieldType.String, value, condition);
    }

    private AdvancedFilter<TModel> AddRule(Operator operatorType, string field, FieldType type, object value,
        Condition condition = Condition.And)
    {
        _rules.Add(new Rule(operatorType, field, type, value, condition));
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

    private Expression CreateExpression(ParameterExpression parameter, Rule rule)
    {
        var member = Expression.Property(parameter, rule.Field);

        switch (rule.Operator)
        {
            case Operator.StartsWith:
                return Expression.Call(member, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }),
                    Expression.Constant(rule.Value.ToString()));

            default:
                throw new NotImplementedException($"Operator {rule.Operator} is not implemented.");
        }
    }

    private Expression CombineExpressions(Expression left, Expression right, Condition condition)
    {
        return condition == Condition.And
            ? Expression.AndAlso(left, right)
            : Expression.OrElse(left, right);
    }
}