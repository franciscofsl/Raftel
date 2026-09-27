using Raftel.Application.Abstractions.Authentication;

namespace Raftel.Application.IntegrationTests.Authentication;

public class TestCurrentUser : ICurrentUser
{
    private readonly HashSet<string> _permissions = new();
    private readonly HashSet<string> _roles = new(); 
    
    public bool IsAuthenticated => true;
    public Guid? UserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
    public string? UserName => "test-user";
    public IEnumerable<string> Roles => _roles;

    public void AddPermission(string permission)
    {
        _permissions.Add(permission);
    }

    public bool HasPermission(string permission)
    {
        return _permissions.Contains(permission);
    }
}