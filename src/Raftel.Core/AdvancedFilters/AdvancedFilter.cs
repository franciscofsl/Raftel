using System.Linq.Expressions;

namespace Raftel.Core.AdvancedFilters;

public class AdvancedFilterBuilder<TModel> : IAdvancedFilterBuilder<TModel>
{
    private readonly List<Rule> _rules = new();
    private readonly Condition _currentCondition;
    private readonly RuleGenerator<TModel> _ruleGenerator;

    public AdvancedFilterBuilder(Condition condition = Condition.And)
    {
        _currentCondition = condition;
        _ruleGenerator = new RuleGenerator<TModel>(_rules, _currentCondition);
    }

    public IAdvancedFilterBuilder<TModel> And(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression)
    {
        var nestedRules = new List<Rule>();
        var nestedBuilder = new RuleGenerator<TModel>(nestedRules, Condition.And);

        filterExpression.Compile().Invoke(nestedBuilder);
        _rules.Add(new Rule(Operator.Nested, string.Empty, FieldType.Nested, null, Condition.And) { Nested = nestedRules });

        return this;
    }

    public IAdvancedFilterBuilder<TModel> Or(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression)
    {
        var nestedRules = new List<Rule>();
        var nestedBuilder = new RuleGenerator<TModel>(nestedRules, Condition.Or);

        filterExpression.Compile().Invoke(nestedBuilder);
        _rules.Add(new Rule(Operator.Nested, string.Empty, FieldType.Nested, null, Condition.Or) { Nested = nestedRules });

        return this;
    }

    public Func<TModel, bool> Build()
    {
        var parameter = Expression.Parameter(typeof(TModel), "model");
        Expression finalExpression = null;

        foreach (var rule in _rules)
        {
            var currentExpression = _ruleGenerator.CreateExpression(parameter, rule);

            if (finalExpression == null)
            {
                finalExpression = currentExpression;
            }
            else
            {
                finalExpression = _ruleGenerator.CombineExpressions(finalExpression, currentExpression, rule.Condition);
            }
        }

        if (finalExpression == null)
        {
            return _ => false;
        }

        var lambda = Expression.Lambda<Func<TModel, bool>>(finalExpression, parameter);
        return lambda.Compile();
    }
}
