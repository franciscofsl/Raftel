namespace Raftel.Core.CustomEntities;

public sealed class CustomFieldKind
{
    private CustomFieldKind(int id, string key)
    {
        Id = id;
        Key = key;
    }

    public int Id { get; private init; }
    public string Key { get; private init; }

    internal enum CustomFieldKindId
    {
        Text,
        Int,
        Decimal,
        Boolean,
        Date,
        Time,
        DateTime
    }

    public static CustomFieldKind Text => new((int)CustomFieldKindId.Text, "Text");
    public static CustomFieldKind Integer => new((int)CustomFieldKindId.Int, "Integer");
    public static CustomFieldKind Decimal => new((int)CustomFieldKindId.Decimal, "Decimal");
    public static CustomFieldKind Boolean => new((int)CustomFieldKindId.Boolean, "Boolean");
    public static CustomFieldKind Date => new((int)CustomFieldKindId.Date, "Date");
    public static CustomFieldKind Time => new((int)CustomFieldKindId.Time, "Time");
    public static CustomFieldKind DateTime => new((int)CustomFieldKindId.DateTime, "DateTime");
}