namespace Raftel.Core.AdvancedFilters;

public enum Operator
{
    StartsWith,
    NotStartsWith,
    EndsWith,
    NotEndsWith,
    Contains,
    NotContains,
    Equal,
    NotEqual,
    In,
    NotIn,
    Null,
    NotNull,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    IsTrue,
    IsFalse,
    DateEqual,
    DateBefore,
    DateAfter,
    Between,
    NotBetween
}