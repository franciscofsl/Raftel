using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Application.Pirates.GetPiratesPaged;

public static class GetPiratesPagedErrors
{
    public static readonly Error PageMustBePositive =
        new("GetPiratesPaged.PageMustBePositive", "Page must be at least 1.");

    public static readonly Error PageSizeMustBePositive =
        new("GetPiratesPaged.PageSizeMustBePositive", "PageSize must be at least 1.");

    public static readonly Error PageSizeExceedsMaximum =
        new("GetPiratesPaged.PageSizeExceedsMaximum", "PageSize cannot exceed 100.");
}
