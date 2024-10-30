using System.Linq.Expressions;
using Raftel.Shared.AdvancedFilters;
using Raftel.Shared.Extensions;

namespace Raftel.Core.AdvancedFilters;

public class RuleGenerator<TModel>(Condition condition) : IFilterRuleBuilder<TModel>
{
    private readonly RuleCollection _rules = new(condition);
    private readonly RuleGeneratorCollection<TModel> _andRuleGenerators = new(Condition.And);
    private readonly RuleGeneratorCollection<TModel> _orRuleGenerators = new(Condition.Or);
    internal readonly Condition Condition = condition;

    public IFilterRuleBuilder<TModel> StartsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.StartsWith, expression, value);
    }

    public IFilterRuleBuilder<TModel> NotStartsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.NotStartsWith, expression, value);
    }

    public IFilterRuleBuilder<TModel> EndsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.EndsWith, expression, value);
    }

    public IFilterRuleBuilder<TModel> NotEndsWith(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.NotEndsWith, expression, value);
    }

    public IFilterRuleBuilder<TModel> Contains(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.Contains, expression, value);
    }

    public IFilterRuleBuilder<TModel> NotContains(Expression<Func<TModel, object>> expression, string value)
    {
        return AddRule(Operator.NotContains, expression, value);
    }

    public IFilterRuleBuilder<TModel> Equal(Expression<Func<TModel, object>> expression, object value)
    {
        return AddRule(Operator.Equal, expression, value);
    }

    public IFilterRuleBuilder<TModel> NotEqual(Expression<Func<TModel, object>> expression, object value)
    {
        return AddRule(Operator.NotEqual, expression, value);
    }

    public IFilterRuleBuilder<TModel> In(Expression<Func<TModel, object>> expression, dynamic values)
    {
        return AddRule(Operator.In, expression, values);
    }

    public IFilterRuleBuilder<TModel> NotIn(Expression<Func<TModel, object>> expression, string[] values)
    {
        return AddRule(Operator.NotIn, expression, values);
    }

    public IFilterRuleBuilder<TModel> Empty(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.Empty, expression, null);
    }

    public IFilterRuleBuilder<TModel> NotEmpty(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.NotEmpty, expression, null);
    }

    public IFilterRuleBuilder<TModel> Null(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.Null, expression, null);
    }

    public IFilterRuleBuilder<TModel> NotNull(Expression<Func<TModel, object>> expression)
    {
        return AddRule(Operator.NotNull, expression, null);
    }

    public IFilterRuleBuilder<TModel> And(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression)
    {
        var ruleGenerator = new RuleGenerator<TModel>(Condition.And);
        filterExpression.Compile().Invoke(ruleGenerator);
        _andRuleGenerators.Add(ruleGenerator);
        return this;
    }

    public IFilterRuleBuilder<TModel> Or(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression)
    {
        var ruleGenerator = new RuleGenerator<TModel>(Condition.Or);
        filterExpression.Compile().Invoke(ruleGenerator);
        _orRuleGenerators.Add(ruleGenerator);
        return this;
    }

    internal Expression ToExpression(ParameterExpression parameter)
    {
        var expression = _rules.ToExpression(parameter);
        expression = AppendOrNestedExpressions(parameter, expression);
        expression = AppendAndNestedExpressions(parameter, expression);
        return expression ?? Expression.Constant(true);
    }

    private IFilterRuleBuilder<TModel> AddRule(Operator operatorType, Expression<Func<TModel, object>> expression,
        object value)
    {
        var body = expression.Body is UnaryExpression unaryExpression
            ? unaryExpression.Operand as MemberExpression
            : expression.Body as MemberExpression;

        var propertyName = body.Member.Name;
        var propertyType = expression.Type;

        var type = propertyType switch
        {
            Type t when t == typeof(int) => FieldType.Integer,
            Type t when t == typeof(decimal) => FieldType.Decimal,
            Type t when t == typeof(bool) => FieldType.Boolean,
            Type t when t == typeof(DateTime) => FieldType.DateTime,
            Type t when t == typeof(string) => FieldType.String,
            _ => FieldType.String
        };
        _rules.Add(new Rule(operatorType, propertyName, type, value, Condition));
        return this;
    }

    private Expression AppendOrNestedExpressions(ParameterExpression parameter, Expression expression)
    {
        var orNestedExpressions = _orRuleGenerators.ToExpression(parameter);
        if (orNestedExpressions is not null)
        {
            expression = expression.Combine(orNestedExpressions, Condition);
        }

        return expression;
    }

    private Expression AppendAndNestedExpressions(ParameterExpression parameter, Expression expression)
    {
        var andNestedExpressions = _andRuleGenerators.ToExpression(parameter);
        if (andNestedExpressions is not null)
        {
            expression = expression.Combine(andNestedExpressions, Condition);
        }

        return expression;
    }
}