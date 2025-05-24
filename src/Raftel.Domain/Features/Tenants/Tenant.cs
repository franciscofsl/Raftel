using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Raftel.Domain.ValueObjects;

namespace Raftel.Domain.Features.Tenants;

public sealed class Tenant : AggregateRoot<TenantId>
{
    private Tenant()
    {
    }

    private Tenant(string name, Code code, string description) : base(TenantId.New())
    {
        Name = name;
        Code = code;
        Description = description;
    }

    public string Name { get; set; }
    public Code Code { get; set; }
    public string Description { get; set; }

    public static Result<Tenant> Create(string name, string code, string description)
    {
        var codeResult = Code.Create(code);
        if (codeResult.IsFailure)
        {
            return Result.Failure<Tenant>(codeResult.Error);
        }

        return Result.Success(new Tenant(name, codeResult.Value, description));
    }
} 