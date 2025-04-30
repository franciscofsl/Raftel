// namespace Raftel.Api.Server.AutoEndpoints;
//
// using System.Reflection;
// using System.Text.RegularExpressions;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Routing;
// using Microsoft.OpenApi.Models;
// using Raftel.Application.Abstractions;
// using Raftel.Application.Commands;
// using Raftel.Domain.Abstractions;
//
// namespace Raftel.Api.Server.AutoEndpoints;
//
// public static class AutoEndpointExtensions
// {
//     public static IEndpointRouteBuilder AddQueryEndpoint<TRequest, TResult>(
//         this IEndpointRouteBuilder app,
//         string route,
//         HttpMethod method)
//         where TRequest : IRequest<Result<TResult>>
//     {
//         var endpoint = method switch
//         {
//             var m when m == HttpMethod.Get => app.MapGet(route, Handler),
//             var m when m == HttpMethod.Post => app.MapPost(route, Handler),
//             _ => throw new NotSupportedException($"HTTP method {method} not supported")
//         };
//  
//         endpoint
//             .WithName($"{method.Method}_{typeof(TRequest).Name}")
//             .WithOpenApi(operation =>
//             {
//                 operation.Parameters = ApiParametersBuilder.Calculate<TRequest>(route);
//                 return operation;
//             });
//
//         return app;
//
//         async Task<IResult> Handler(HttpContext context, IRequestDispatcher dispatcher)
//         {
//             var request = BuildRequestFromRouteAndQuery<TRequest>(context);
//
//             var result = await dispatcher.DispatchAsync<TRequest, Result<TResult>>(request);
//
//             return result.IsSuccess
//                 ? Results.Ok(result.Value)
//                 : Results.BadRequest(result.Error);
//         }
//     }
//
//     public static IEndpointRouteBuilder AddCommandEndpoint<TRequest>(
//         this IEndpointRouteBuilder app,
//         string route,
//         HttpMethod method)
//         where TRequest : IRequest<Result>
//     {
//         var requestType = typeof(TRequest);
//
//         async Task<IResult> Handler(HttpContext context, IRequestDispatcher dispatcher)
//         {
//             TRequest request;
//
//             if (typeof(ICommand).IsAssignableFrom(requestType) ||
//                 (requestType.IsGenericType && requestType.GetGenericTypeDefinition() == typeof(ICommand)))
//             {
//                 request = await context.Request.ReadFromJsonAsync<TRequest>()
//                           ?? throw new InvalidOperationException($"Invalid body for {requestType.Name}");
//             }
//             else
//             {
//                 throw new NotSupportedException("Only commands are supported");
//             }
//
//             var result = await dispatcher.DispatchAsync<TRequest, Result>(request);
//
//             return result.IsSuccess
//                 ? Results.Ok(result)
//                 : Results.BadRequest(result.Error);
//         }
//
//         var endpoint = method switch
//         {
//             var m when m == HttpMethod.Get => app.MapGet(route, Handler),
//             var m when m == HttpMethod.Post => app.MapPost(route, Handler),
//             var m when m == HttpMethod.Put => app.MapPut(route, Handler),
//             var m when m == HttpMethod.Delete => app.MapDelete(route, Handler),
//             _ => throw new NotSupportedException($"HTTP method {method} not supported")
//         };
//
//         var routeParameters = Regex.Matches(route, "{(.*?)}")
//             .Select(m => m.Groups[1].Value)
//             .ToArray();
//
//         endpoint
//             .WithName($"{method.Method}_{typeof(TRequest).Name}")
//             .Accepts<TRequest>("application/json")
//             .WithOpenApi(operation =>
//             {
//                 var properties = typeof(TRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance);
//                 var propertyDict =
//                     properties.ToDictionary(p => p.Name, p => p.PropertyType, StringComparer.OrdinalIgnoreCase);
//
//                 foreach (var param in routeParameters)
//                 {
//                     if (!propertyDict.TryGetValue(param, out var paramType))
//                     {
//                         throw new InvalidOperationException(
//                             $"Parameter '{param}' not found in '{typeof(TRequest).Name}' properties");
//                     }
//
//                     var (type, format) = MapToOpenApiType(paramType);
//
//                     operation.Parameters.Add(new OpenApiParameter
//                     {
//                         Name = param,
//                         In = ParameterLocation.Path,
//                         Required = true,
//                         Schema = new OpenApiSchema
//                         {
//                             Type = type,
//                             Format = format
//                         }
//                     });
//                 }
//
//                 return operation;
//             });
//
//         return app;
//     }
//
//     private static TRequest BuildRequestFromRouteAndQuery<TRequest>(HttpContext context)
//     {
//         var requestType = typeof(TRequest);
//         var constructor = requestType.GetConstructors().FirstOrDefault();
//         if (constructor == null)
//             throw new InvalidOperationException($"No public constructor found for {requestType.Name}");
//
//         var parameters = constructor.GetParameters();
//
//         var args = new object?[parameters.Length];
//         for (var i = 0; i < parameters.Length; i++)
//         {
//             var param = parameters[i];
//             var name = param.Name!;
//
//             object? value = null;
//
//             if (context.Request.RouteValues.TryGetValue(name, out var routeValue) && routeValue != null)
//             {
//                 value = ConvertSimpleType(routeValue, param.ParameterType);
//             }
//             else if (context.Request.Query.TryGetValue(name, out var queryValue) &&
//                      !string.IsNullOrWhiteSpace(queryValue))
//             {
//                 value = ConvertSimpleType(queryValue.ToString(), param.ParameterType);
//             }
//             else if (IsNullable(param.ParameterType))
//             {
//                 value = null;
//             }
//             else
//             {
//                 throw new InvalidOperationException($"Missing required parameter '{name}'");
//             }
//
//             args[i] = value;
//         }
//
//         var instance = (TRequest)constructor.Invoke(args);
//         return instance;
//     }
//
//     private static object? ConvertSimpleType(object value, Type targetType)
//     {
//         if (targetType == typeof(Guid) && value is string s)
//         {
//             return Guid.Parse(s);
//         }
//
//         if (targetType.IsEnum)
//         {
//             return Enum.Parse(targetType, value.ToString()!);
//         }
//
//         return Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
//     }
//
//     private static bool IsNullable(Type type)
//     {
//         return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
//     }
//
//     private static (string Type, string? Format) MapToOpenApiType(Type type)
//     {
//         type = Nullable.GetUnderlyingType(type) ?? type;
//
//         if (type == typeof(Guid))
//             return ("string", "uuid");
//         if (type == typeof(DateTime))
//             return ("string", "date-time");
//         if (type == typeof(int) || type == typeof(long))
//             return ("integer", null);
//         if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
//             return ("number", "double");
//         if (type == typeof(bool))
//             return ("boolean", null);
//
//         return ("string", null);
//     }
// }


// IN TESTS: // app.AddCommandEndpoint<CreatePirateCommand>("/api/pirates", HttpMethod.Post);
