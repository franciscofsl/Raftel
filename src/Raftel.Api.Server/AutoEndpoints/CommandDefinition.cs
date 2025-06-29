namespace Raftel.Api.Server.AutoEndpoints;

public record CommandDefinition(Type Request, string Route, HttpMethod Method);