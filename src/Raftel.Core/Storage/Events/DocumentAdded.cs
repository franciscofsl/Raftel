using Raftel.Core.Events;

namespace Raftel.Core.Storage.Events;

public record DocumentAdded(string Pointer, byte[] Content) : IDomainEvent;