using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Domain.Pirates;

public static class PirateErrors
{
    public static Error LuffyShouldBeThePirateKing =>
        Error.Validation("Pirate.Name", "Luffy should be The Pirate King.");

    public static Error CannotEatMoreThanOneDevilFruit =>
        Error.Conflict("Pirate.EatenDevilFruits", "Pirate cannot eat more than one Devil Fruit.");
}