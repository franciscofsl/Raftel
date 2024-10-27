namespace Raftel.Core.AdvancedFilters;

public interface IAdvancedFilterBuilder<TModel> : IAdvancedFilter<TModel>
{
    Func<TModel, bool> Build();
}