namespace Raftel.Api.Server.AutoEndpoints;

public record QueryDefinition(Type Request, Type Result, string Route, HttpMethod Method);