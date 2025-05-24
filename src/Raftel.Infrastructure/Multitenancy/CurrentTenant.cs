using Raftel.Application.Abstractions.Multitenancy;

namespace Raftel.Infrastructure.Multitenancy;

internal sealed class CurrentTenant : ICurrentTenant
{
    private static readonly AsyncLocal<Guid?> CurrentTenantId = new();

    public Guid? Id => CurrentTenantId.Value;

    public IDisposable Change(Guid? tenantId)
    {
        return new TenantScope(tenantId);
    }

    private sealed class TenantScope : IDisposable
    {
        private readonly Guid? _previousTenantId;

        public TenantScope(Guid? tenantId)
        {
            _previousTenantId = CurrentTenantId.Value;
            CurrentTenantId.Value = tenantId;
        }

        public void Dispose()
        {
            CurrentTenantId.Value = _previousTenantId;
        }
    }
} 