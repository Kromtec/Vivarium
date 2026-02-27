using Microsoft.Xna.Framework;

namespace Vivarium.Entities;

public struct Structure : IGridEntity
{
    public long Id { get; set; } // Unique identifier for tracking across generations
    public int Index { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    private Color originalColor;
    public Color OriginalColor
    {
        readonly get => originalColor;
        set
        {
            originalColor = value;
            Color = value;
        }
    }
    public Color Color { get; private set; }


    public static Structure Create(int index, int x, int y)
    {
        return ConstructStructure(index, x, y);
    }

    private static Structure ConstructStructure(int index, int x, int y)
    {
        return new Structure()
        {
            Id = VivariumGame.NextEntityId++,
            Index = index,
            X = x,
            Y = y,
            OriginalColor = Visuals.VivariumColors.Structure,
        };
    }
}
