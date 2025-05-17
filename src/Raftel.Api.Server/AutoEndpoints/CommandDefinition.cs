namespace Raftel.Api.Server.AutoEndpoints;

public record CommandDefinition(Type Request, string Route, HttpMethod Method)
{
    internal bool IsAnonymous { get; private set; } = false;

    public CommandDefinition AllowAnonymous()
    {
        IsAnonymous = true;
        return this;
    }
};