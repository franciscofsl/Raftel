using System.Linq.Expressions;

namespace Raftel.Core.AdvancedFilters;

public interface IAdvancedFilter<TModel>
{
    public IAdvancedFilterBuilder<TModel> And(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression);

    public IAdvancedFilterBuilder<TModel>
        Or(Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression);
}