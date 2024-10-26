namespace Raftel.Core.AdvancedFilters;

public class Rule(Operator Operator, string Field, FieldType Type, object Value, Condition Condition = Condition.And);