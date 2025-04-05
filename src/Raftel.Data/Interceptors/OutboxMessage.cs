using System.Text.Json;
using System.Text.Json.Serialization;
using Raftel.Core.BaseTypes;

namespace Raftel.Data.Interceptors;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    public string Type { get; init; }

    public string Content { get; init; }

    public DateTime OccurredOn { get; init; }

    public DateTime? ProcessedOn { get; set; }

    public IDomainEvent ToDomainEvent()
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter()
            },
            PropertyNameCaseInsensitive = false
        };

        return JsonSerializer.Deserialize<IDomainEvent>(Content, options);
    }
}