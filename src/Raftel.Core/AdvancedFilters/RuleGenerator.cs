using System.Linq.Expressions;

namespace Raftel.Core.AdvancedFilters;

public class RuleGenerator<TModel> : IFilterRuleBuilder<TModel>
{
    private readonly RuleCollection _rules;
    private readonly List<RuleGenerator<TModel>> _andNestedRules = new();
    private readonly List<RuleGenerator<TModel>> _orNestedGenerators = new();
    private readonly Condition _currentCondition;

    public RuleGenerator(Condition condition)
    {
        _currentCondition = condition;
        _rules = new RuleCollection(condition);
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
        var ruleGenerator = new RuleGenerator<TModel>(Condition.And);
        filterExpression.Compile().Invoke(ruleGenerator);
        _andNestedRules.Add(ruleGenerator);
        return this;
    }

    public IFilterRuleBuilder<TModel> Or(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression)
    {
        var ruleGenerator = new RuleGenerator<TModel>(Condition.Or);
        filterExpression.Compile().Invoke(ruleGenerator);
        _orNestedGenerators.Add(ruleGenerator);
        return this;
    } 

    internal Expression CreateExpression(ParameterExpression parameter)
    {
        var expression = _rules.ToExpression(parameter);

        var orNestedExpressions = CreateOrNestedExpressions(parameter);
        if (orNestedExpressions is not null)
        {
            expression = CombineExpressions(expression, orNestedExpressions);
        }

        var andNestedExpressions = CreateAndNestedExpressions(parameter);
        if (andNestedExpressions is not null)
        {
            expression = CombineExpressions(expression, andNestedExpressions);
        }

        return expression ?? Expression.Constant(true);
    }

    private Expression CreateOrNestedExpressions(ParameterExpression parameter)
    {
        Expression orExpression = null;
        foreach (var orNestedGenerator in _orNestedGenerators)
        {
            var nested = orNestedGenerator.CreateExpression(parameter);
            if (orExpression is null)
            {
                orExpression = nested;
                continue;
            }

            orExpression = CombineExpressions(orExpression, nested);
        }

        return orExpression;
    }

    private Expression CreateAndNestedExpressions(ParameterExpression parameter)
    {
        Expression orExpression = null;
        foreach (var orNestedGenerator in _andNestedRules)
        {
            var nested = orNestedGenerator.CreateExpression(parameter);
            if (orExpression is null)
            {
                orExpression = nested;
                continue;
            }

            orExpression = CombineExpressions(orExpression, nested);
        }

        return orExpression;
    }

    public Expression CombineExpressions(Expression left, Expression right)
    {
        return _currentCondition == Condition.And
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