using Microsoft.Extensions.Localization;
using Raftel.Blazor.Localization.Services;
using Raftel.Blazor.Services.LocalStorage;
using Raftel.Blazor.Toast;

namespace Raftel.Blazor;

public abstract partial class SnComponentBase : ComponentBase, IDisposable
{
    [Inject] protected ILocalStorageService LocalStorage { get; set; }

    [Parameter] public string StateStoreKey { get; set; }

    [Inject] protected ToastService Toast { get; set; }
    
    [Inject] protected IStringLocalizer L { get; set; }

    public virtual void Dispose()
    {
    }
}