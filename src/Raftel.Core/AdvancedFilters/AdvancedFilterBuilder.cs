namespace Raftel.Core.AdvancedFilters;

public static class AdvancedFilterBuilder
{ 
    public static IAdvancedFilterBuilder<TModel> ForModel<TModel>()
    {
        return new AdvancedFilterBuilder<TModel>();
    }
}