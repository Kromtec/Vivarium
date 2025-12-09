using System;

namespace Vivarium.World;

public struct GridCell(EntityType type, int index) : IEquatable<GridCell>
{
    public static GridCell Empty => new(EntityType.Empty, -1);

    public EntityType Type { get; } = type;
    public int Index { get; } = index;

    public static bool operator ==(GridCell left, GridCell right)
    {
        return left.Type == right.Type && left.Index == right.Index;
    }

    public static bool operator !=(GridCell left, GridCell right)
    {
        return !(left == right);
    }

    public bool Equals(GridCell other)
    {
        return Type == other.Type && Index == other.Index;
    }

    public override bool Equals(object? obj)
    {
        return obj is GridCell other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Index);
    }
}