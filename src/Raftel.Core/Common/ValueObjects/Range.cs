namespace Raftel.Core.Common.ValueObjects;

public readonly struct Range<T> where T : struct, IComparable<T>
{
    public T? Min { get; }
    public T? Max { get; }

    public Range(T? min, T? max)
    {
        if (min.HasValue && max.HasValue && min.Value.CompareTo(max.Value) > 0)
        {
            throw new ArgumentException("Min cannot be greater than Max.");
        }

        Min = min;
        Max = max;
    }

    public bool IsWithinRange(T value)
    {
        var minCheck = !Min.HasValue || value.CompareTo(Min.Value) >= 0;
        var maxCheck = !Max.HasValue || value.CompareTo(Max.Value) <= 0;

        return minCheck && maxCheck;
    }

    public override string ToString()
    {
        return $"Range: [{Min}, {Max}]";
    }

    public override bool Equals(object obj)
    {
        if (obj is Range<T> other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Min, Max);
    }

    public static bool operator ==(Range<T> left, Range<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Range<T> left, Range<T> right)
    {
        return !(left == right);
    }
}