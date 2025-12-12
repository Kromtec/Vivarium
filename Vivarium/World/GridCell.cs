#nullable enable
using System;

namespace Vivarium.World;

public readonly struct GridCell(EntityType type, int index) : IEquatable<GridCell>
{
    public static GridCell Empty => default;

    public EntityType Type { get; } = type;
    public int Index { get; } = index;

    public static bool operator ==(GridCell left, GridCell right)
    {
        if (left.Type == EntityType.Empty && right.Type == EntityType.Empty)
        {
            return true;
        }
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