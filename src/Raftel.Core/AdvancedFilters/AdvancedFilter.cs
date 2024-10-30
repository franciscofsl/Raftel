using System.Linq.Expressions;
using Raftel.Shared.AdvancedFilters;
using Raftel.Shared.Extensions;

namespace Raftel.Core.AdvancedFilters;

public class AdvancedFilterBuilder<TModel> : IAdvancedFilterBuilder<TModel>
{
    private readonly List<RuleGenerator<TModel>> _generators = new();
    private readonly Condition _currentCondition;

    public AdvancedFilterBuilder(Condition condition = Condition.And)
    {
        _currentCondition = condition;
    }

    public IAdvancedFilterBuilder<TModel> And(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression)
    {
        var nestedBuilder = new RuleGenerator<TModel>(Condition.And);
        filterExpression.Compile().Invoke(nestedBuilder);
        _generators.Add(nestedBuilder);
        return this;
    }

    public IAdvancedFilterBuilder<TModel> Or(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression)
    {
        var nestedBuilder = new RuleGenerator<TModel>(Condition.Or);
        filterExpression.Compile().Invoke(nestedBuilder);
        _generators.Add(nestedBuilder);
        return this;
    }

    public Func<TModel, bool> Build()
    {
        var parameter = Expression.Parameter(typeof(TModel), "model");
        Expression finalExpression = null;

        foreach (var generator in _generators)
        {
            var currentExpression = generator.CreateExpression(parameter);

            if (finalExpression == null)
            {
                finalExpression = currentExpression;
            }
            else
            {
                finalExpression = finalExpression.Combine(currentExpression, generator.Condition);
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