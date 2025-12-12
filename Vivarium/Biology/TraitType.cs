namespace Vivarium.Biology;

public static partial class Genetics
{
    /// <summary>
    /// Enum describing trait indices. Order determines mapping into the trait gene region.
    /// Index 0 maps to the last 2 genes, index 1 to the second-to-last pair, etc.
    /// </summary>
    public enum TraitType
    {
        Strength = 0,
        Bravery = 1,
        MetabolicEfficiency = 2,
        Perception = 3,
        Speed = 4,
        TrophicBias = 5,
        Constitution = 6
    }
}