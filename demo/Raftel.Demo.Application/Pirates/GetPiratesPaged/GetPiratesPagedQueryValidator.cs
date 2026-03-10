using Raftel.Domain.Validators;

namespace Raftel.Demo.Application.Pirates.GetPiratesPaged;

public class GetPiratesPagedQueryValidator : Validator<GetPiratesPagedQuery>
{
    public GetPiratesPagedQueryValidator()
    {
        EnsureThat(q => q.Page >= 1, GetPiratesPagedErrors.PageMustBePositive);
        EnsureThat(q => q.PageSize >= 1, GetPiratesPagedErrors.PageSizeMustBePositive);
        EnsureThat(q => q.PageSize <= 100, GetPiratesPagedErrors.PageSizeExceedsMaximum);
    }
}
