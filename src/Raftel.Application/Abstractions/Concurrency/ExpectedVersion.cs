namespace Raftel.Application.Abstractions.Concurrency;

/// <summary>
/// Default <see cref="IExpectedVersion"/>, registered with scoped lifetime.
/// </summary>
public sealed class ExpectedVersion : IExpectedVersion
{
    /// <inheritdoc />
    public string Value { get; private set; }

    /// <inheritdoc />
    public void Expect(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        Value = version;
    }

    /// <inheritdoc />
    public string Consume()
    {
        var version = Value;
        Value = null;
        return version;
    }
}
