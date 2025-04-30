using Microsoft.OpenApi.Models;

namespace Raftel.Api.Server.AutoEndpoints;

internal sealed class OpenApiType
{
    private readonly string _typeName;
    private readonly string _format;

    private OpenApiType(string typeName, string format)
    {
        _typeName = typeName;
        _format = format;
    }

    public static OpenApiType FromType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(Guid))
        {
            return new OpenApiType("string", "uuid");
        }

        if (type == typeof(DateTime))
        {
            return new OpenApiType("string", "date-time");
        }

        if (type == typeof(int) || type == typeof(long))
        {
            return new OpenApiType("integer", null);
        }

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            return new OpenApiType("number", "double");
        }

        if (type == typeof(bool))
        {
            return new OpenApiType("boolean", null);
        }

        return new OpenApiType("string", null);
    }

    public OpenApiSchema ToSchema()
    {
        return new OpenApiSchema
        {
            Type = _typeName,
            Format = _format
        };
    }
}