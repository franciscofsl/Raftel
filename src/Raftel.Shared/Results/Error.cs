namespace Raftel.Shared.Results;

public record class Error(string Code)
{
    public static readonly Error None = new(string.Empty);

    public override string ToString() => Code;

    public static implicit operator string(Error error) => error.Code;

    public static implicit operator Error(string code) => new Error(code);

    public virtual bool Equals(Error other)
    {
        return other?.Code == Code;
    }
}