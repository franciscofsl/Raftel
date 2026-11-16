using Raftel.Application.Authorization;

namespace Raftel.Demo.Application.Pirates;

[PermissionSet]
public class PiratesPermissions
{
    public const string Management = "Pirates.Management";
    public const string View = "Pirates.View";
}