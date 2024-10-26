namespace Raftel.Core.AdvancedFilters;

public class AdvancedFilter
{
    public Condition Condition { get; set; }
    public List<Rule> Rules { get; set; } = new();
}