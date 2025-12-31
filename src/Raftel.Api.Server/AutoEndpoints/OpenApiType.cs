using Microsoft.OpenApi;

namespace Raftel.Api.Server.AutoEndpoints;

internal sealed class OpenApiType
{
    private readonly JsonSchemaType _typeName;
    private readonly string _format;

    private OpenApiType(JsonSchemaType typeName, string format)
    {
        _typeName = typeName;
        _format = format;
    }

    public static OpenApiType FromType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(Guid))
        {
            return new OpenApiType(JsonSchemaType.String, "uuid");
        }

        if (type == typeof(DateTime))
        {
            return new OpenApiType(JsonSchemaType.String, "date-time");
        }

        if (type == typeof(int) || type == typeof(long))
        {
            return new OpenApiType(JsonSchemaType.Integer, null);
        }

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            return new OpenApiType(JsonSchemaType.Number, null);
        }

        if (type == typeof(bool))
        {
            return new OpenApiType(JsonSchemaType.Boolean, null);
        }

        return new OpenApiType(JsonSchemaType.String, null);
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