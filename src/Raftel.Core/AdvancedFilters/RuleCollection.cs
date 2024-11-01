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
        var constantValue = Expression.Constant(rule.Value);
        var isNotNull = CreateNullCheckExpression(member);
        var containsMethod = GetContainsMethod(rule.Value);

        return rule.Operator switch
        {
            Operator.StartsWith => GenerateStartsWithExpression(isNotNull, member, constantValue),
            Operator.NotStartsWith => GenerateNotStartsWithExpression(isNotNull, member, constantValue),
            Operator.EndsWith => GenerateEndsWithExpression(isNotNull, member, constantValue),
            Operator.NotEndsWith => GenerateNotEndsWithExpression(isNotNull, member, constantValue),
            Operator.Contains => GenerateContainsExpression(isNotNull, member, constantValue),
            Operator.NotContains => GenerateNotContainsExpression(isNotNull, member, constantValue),
            Operator.Equal => Expression.Equal(member, constantValue),
            Operator.NotEqual => Expression.Not(Expression.Equal(member, constantValue)),
            Operator.In => GenerateInExpression(isNotNull, containsMethod, constantValue, member),
            Operator.NotIn => GenerateNotInExpression(isNotNull, containsMethod, constantValue, member),
            Operator.Empty => Expression.AndAlso(isNotNull,
                Expression.Equal(member, Expression.Constant(string.Empty))),
            Operator.NotEmpty => Expression.AndAlso(isNotNull,
                Expression.NotEqual(member, Expression.Constant(string.Empty))),
            Operator.Null => Expression.Equal(member, Expression.Constant(null)),
            Operator.NotNull => Expression.NotEqual(member, Expression.Constant(null)),
            _ => throw new NotImplementedException($"Operator {rule.Operator} is not implemented.")
        };
    }

    private Expression CreateNullCheckExpression(MemberExpression member)
    {
        return member.Type.IsValueType && Nullable.GetUnderlyingType(member.Type) == null
            ? null
            : Expression.NotEqual(member, Expression.Constant(null));
    }

    private MethodInfo GetContainsMethod(object value)
    {
        if (value is null)
        {
            return null;
        }

        var elementType = value.GetType().IsArray
            ? value.GetType().GetElementType()
            : value.GetType().GetGenericArguments().FirstOrDefault() ?? value.GetType();

        return typeof(Enumerable).GetMethods()
            .FirstOrDefault(m => m.Name == "Contains" && m.GetParameters().Length == 2)?
            .MakeGenericMethod(elementType);
    }

    private static BinaryExpression GenerateNotContainsExpression(Expression isNotNull, MemberExpression member,
        ConstantExpression constantValue)
    {
        var containsExpression = GenerateContainsExpression(isNotNull, member, constantValue);
        return Expression.AndAlso(isNotNull, Expression.Not(containsExpression));
    }

    private static Expression GenerateStartsWithExpression(Expression isNotNull, Expression member,
        ConstantExpression constantValue)
    {
        return Expression.AndAlso(isNotNull, Expression.Call(member, nameof(Operator.StartsWith), null, constantValue));
    }

    private static Expression GenerateNotStartsWithExpression(Expression isNotNull, MemberExpression member,
        ConstantExpression constantValue)
    {
        var startsWithExpression = GenerateStartsWithExpression(isNotNull, member, constantValue);
        return Expression.AndAlso(isNotNull, Expression.Not(startsWithExpression));
    }

    private static Expression GenerateEndsWithExpression(Expression isNotNull, MemberExpression member,
        ConstantExpression constantValue)
    {
        return Expression.AndAlso(isNotNull, Expression.Call(member, nameof(Operator.EndsWith), null, constantValue));
    }

    private static Expression GenerateNotEndsWithExpression(Expression isNotNull, MemberExpression member,
        ConstantExpression constantValue)
    {
        var endsWithExpression = GenerateEndsWithExpression(isNotNull, member, constantValue);
        return Expression.AndAlso(isNotNull, Expression.Not(endsWithExpression));
    }

    private static Expression GenerateContainsExpression(Expression isNotNull, MemberExpression member,
        ConstantExpression constantValue)
    {
        return Expression.AndAlso(isNotNull, Expression.Call(member, nameof(Operator.Contains), null, constantValue));
    }

    private static Expression GenerateInExpression(Expression isNotNull, MethodInfo containsMethod,
        ConstantExpression constantValue, MemberExpression member)
    {
        return isNotNull != null
            ? Expression.AndAlso(isNotNull, Expression.Call(containsMethod, constantValue, member))
            : Expression.Call(containsMethod, constantValue, member);
    }

    private static Expression GenerateNotInExpression(Expression isNotNull, MethodInfo containsMethod,
        ConstantExpression constantValue, MemberExpression member)
    {
        return isNotNull != null
            ? Expression.AndAlso(isNotNull, Expression.Not(Expression.Call(containsMethod, constantValue, member)))
            : Expression.Not(Expression.Call(containsMethod, constantValue, member));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}