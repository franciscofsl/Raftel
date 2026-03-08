namespace Raftel.Api.Server.AutoEndpoints;

public record CommandDefinition(Type Request, Type? Result, string Route, HttpMethod Method);