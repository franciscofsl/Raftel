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
        FieldType type, object value)
    {
        var body = expression.Body as MemberExpression;
        var propertyName = body.Member.Name;
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