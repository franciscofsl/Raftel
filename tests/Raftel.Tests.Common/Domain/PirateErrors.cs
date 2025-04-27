using Raftel.Domain.Abstractions;

namespace Raftel.Tests.Common.Domain;

public static class PirateErrors
{
    public static Error LuffyShouldBeThePirateKing => new Error("Pirate.Name", "Luffy should be The Pirate King.");
}