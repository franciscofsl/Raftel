using System.Diagnostics.CodeAnalysis;
using Raftel.Application;
using Raftel.Demo.Core;
using Raftel.Shared.Modules;

namespace Raftel.Demo.Application;

[ExcludeFromCodeCoverage]
[ModulesToInclude(typeof(DemoCoreModule))]
public sealed class SamplenetApplicationModule : RaftelApplicationModule
{
}