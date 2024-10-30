using System.Collections;
using System.Linq.Expressions;
using Raftel.Shared.AdvancedFilters;
using Raftel.Shared.Extensions;

namespace Raftel.Core.AdvancedFilters;

public class RuleCollection(Condition condition) : IEnumerable<Rule>
{
    private readonly List<Rule> _rules = new();

    public void Add(Rule rule)
    {
        _rules.Add(rule);
    }

    public IEnumerator<Rule> GetEnumerator()
    {
        return _rules.GetEnumerator();
    }

    public Expression ToExpression(ParameterExpression parameter)
    {
        Expression expression = null;
        foreach (var rule in _rules)
        {
            var nested = CreateExpression(parameter, rule);

            if (expression is null)
            {
                expression = nested;
                continue;
            }

            expression = expression.Combine(nested, condition);
        }

        return expression;
    }

    private Expression CreateExpression(ParameterExpression parameter, Rule rule)
    {
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

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}