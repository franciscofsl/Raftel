namespace Raftel.Core.CustomEntities.CustomFieldsTypes;

public sealed class EqualityKind
{
    private EqualityKind()
    {
        /* For ORM or serialization */
    }

    private EqualityKind(int kind, string key)
    {
        Id = kind;
        Key = key;
    }

    public int Id { get; }
    public string Key { get; }

    private enum EqualityTypeKind
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual,
        Contains,
        DoesNotContain,
        StartsWith,
        EndsWith
    }

    public static EqualityKind Equal => new EqualityKind((int)EqualityTypeKind.Equal, "Equal");
    public static EqualityKind NotEqual => new EqualityKind((int)EqualityTypeKind.NotEqual, "NotEqual");
    public static EqualityKind GreaterThan => new EqualityKind((int)EqualityTypeKind.GreaterThan, "GreaterThan");
    public static EqualityKind LessThan => new EqualityKind((int)EqualityTypeKind.LessThan, "LessThan");

    public static EqualityKind GreaterOrEqual =>
        new EqualityKind((int)EqualityTypeKind.GreaterOrEqual, "GreaterOrEqual");

    public static EqualityKind LessOrEqual => new EqualityKind((int)EqualityTypeKind.LessOrEqual, "LessOrEqual");
    public static EqualityKind Contains => new EqualityKind((int)EqualityTypeKind.Contains, "Contains");

    public static EqualityKind DoesNotContain =>
        new EqualityKind((int)EqualityTypeKind.DoesNotContain, "DoesNotContain");

    public static EqualityKind StartsWith => new EqualityKind((int)EqualityTypeKind.StartsWith, "StartsWith");
    public static EqualityKind EndsWith => new EqualityKind((int)EqualityTypeKind.EndsWith, "EndsWith");

    public static IReadOnlyList<EqualityKind> All => new[]
    {
        Equal, NotEqual, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual, Contains, DoesNotContain, StartsWith,
        EndsWith
    };

    public override bool Equals(object obj)
    {
        return obj is EqualityKind otherKind && Id == otherKind.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static EqualityKind ById(int id)
    {
        return All.FirstOrDefault(_ => _.Id == id);
    }

    public static EqualityKind ByKey(string key)
    {
        return All.FirstOrDefault(_ => _.Key == key);
    }
}