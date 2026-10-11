using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Raftel.Domain.BaseTypes;

namespace Raftel.Infrastructure.Outbox;

/// <summary>
/// Serializes strongly-typed GUID identifiers as plain GUID strings. Without it they would serialize as
/// an empty object, because the underlying value is private.
/// </summary>
internal sealed class TypedGuidIdJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsClass && !typeToConvert.IsAbstract && typeof(TypedGuidId).IsAssignableFrom(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert))!;

    private sealed class Converter<TId> : JsonConverter<TId> where TId : TypedGuidId
    {
        public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetGuid();

            try
            {
                return (TId)Activator.CreateInstance(typeToConvert, value)!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                throw new JsonException(exception.InnerException.Message, exception.InnerException);
            }
        }

        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue((Guid)(TypedId<Guid>)value);
        }
    }
}
