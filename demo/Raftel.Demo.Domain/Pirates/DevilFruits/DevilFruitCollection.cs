namespace Raftel.Demo.Domain.Pirates.DevilFruits;

internal class DevilFruitCollection
{
    private readonly List<DevilFruit> _fruits = new();

    internal void Add(DevilFruit fruit) => _fruits.Add(fruit);

    internal bool HasAny() => _fruits.Any();

    internal IReadOnlyCollection<DevilFruit> AsReadOnly() => _fruits.AsReadOnly();
}