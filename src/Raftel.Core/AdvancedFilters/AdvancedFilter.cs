namespace Raftel.Core.AdvancedFilters;

public class AdvancedFilter
{
    private AdvancedFilter()
    {
    }

    public static AdvancedFilter<TModel> ForModel<TModel>(Condition condition = Condition.And)
    {
        return new AdvancedFilter<TModel>(condition);
    }
}

public class AdvancedFilter<TModel>(Condition Condition = Condition.And)
{
    private List<Rule> _rules = new();

    public AdvancedFilter<TModel> StartsWith(Func<TModel, object> func, string value)
    {
        return this;
    }

    public bool Satisfy(TModel model)
    {
        return true;
    }
}