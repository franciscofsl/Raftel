using System.Collections;
using System.Linq.Expressions;
using Raftel.Shared.AdvancedFilters;
using Raftel.Shared.Extensions;

namespace Raftel.Core.AdvancedFilters;

public class RuleGeneratorCollection<TModel>(Condition condition) : IEnumerable<RuleGenerator<TModel>>
{
    private readonly List<RuleGenerator<TModel>> _ruleGenerators = new();

    public IEnumerator<RuleGenerator<TModel>> GetEnumerator()
    {
        return _ruleGenerators.GetEnumerator();
    }

    public void Add(RuleGenerator<TModel> ruleGenerator)
    {
        _ruleGenerators.Add(ruleGenerator);
    }
    
    internal Expression ToExpression(ParameterExpression parameter)
    {
        Expression orExpression = null;
        foreach (var orNestedGenerator in _ruleGenerators)
        {
            var nested = orNestedGenerator.ToExpression(parameter);
            if (orExpression is null)
            {
                orExpression = nested;
                continue;
            }

            orExpression = orExpression.Combine(nested, condition);
        }

        return orExpression;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}