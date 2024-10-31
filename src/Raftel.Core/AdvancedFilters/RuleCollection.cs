using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
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

        var elementType = rule.Value.GetType().IsArray
            ? rule.Value.GetType().GetElementType()
            : rule.Value.GetType().GetGenericArguments().FirstOrDefault() ?? rule.Value.GetType();

        var constantValue = CalculateConstantValueFromRule(rule, elementType);

        var isNotNull = member.Type.IsValueType && Nullable.GetUnderlyingType(member.Type) == null
            ? null
            : Expression.NotEqual(member, Expression.Constant(null));

        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

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

            Operator.In => isNotNull != null
                ? Expression.AndAlso(isNotNull, Expression.Call(containsMethod, constantValue, member))
                : Expression.Call(containsMethod, constantValue, member),

            Operator.NotIn => isNotNull != null
                ? Expression.AndAlso(isNotNull, Expression.Not(Expression.Call(containsMethod, constantValue, member)))
                : Expression.Not(Expression.Call(containsMethod, constantValue, member)),

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

    private static ConstantExpression CalculateConstantValueFromRule(Rule rule, Type elementType)
    {
        var constantValue = Expression.Constant(rule.Value);
        if (rule.Value is IEnumerable collection)
        {
            constantValue = Expression.Constant(collection, typeof(IEnumerable<>).MakeGenericType(elementType));
        }
        else if (elementType.IsAssignableFrom(typeof(Enumerable)))
        {
            constantValue =
                Expression.Constant(new[] { rule.Value }, typeof(IEnumerable<>).MakeGenericType(elementType));
        }

        return constantValue;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}