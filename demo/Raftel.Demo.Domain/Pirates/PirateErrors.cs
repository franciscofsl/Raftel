using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Domain.Pirates;

public static class PirateErrors
{
    public static Error LuffyShouldBeThePirateKing => new Error("Pirate.Name", "Luffy should be The Pirate King.");
}