using Raftel.Core.BaseTypes;

namespace Raftel.Inkventory.Core.Customers;

public sealed record CustomerCreated(CustomerId CustomerId, Name Name, FirstLastName FirstLastName) : DomainEvent;