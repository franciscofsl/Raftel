using Raftel.Shared.AdvancedFilters;

namespace Raftel.Core.AdvancedFilters;

public record Rule(Operator Operator, string Field, FieldType Type, object Value, Condition Condition = Condition.And);