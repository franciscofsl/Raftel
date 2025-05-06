using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Domain.Pirates;

public static class PirateErrors
{
    public static Error LuffyShouldBeThePirateKing => new("Pirate.Name", "Luffy should be The Pirate King.");

    public static Error CannotEatMoreThanOneDevilFruit =>
        new("Pirate.EatenDevilFruits", "Pirate cannot eat more than one Devil Fruit.");
}