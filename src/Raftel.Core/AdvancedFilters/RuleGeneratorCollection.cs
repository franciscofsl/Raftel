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
        Expression expression = null;
        foreach (var generator in _ruleGenerators)
        {
            var nested = generator.ToExpression(parameter);
            if (expression is null)
            {
                expression = nested;
                continue;
            }

            expression = expression.Combine(nested, condition);
        }

        return expression;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}