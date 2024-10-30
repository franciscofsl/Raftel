namespace Raftel.Core.AdvancedFilters;

public class AdvancedFilterBuilder
{
    private AdvancedFilterBuilder()
    {
    }

    public static IAdvancedFilterBuilder<TModel> ForModel<TModel>()
    {
        return new AdvancedFilterBuilder<TModel>();
    }
}