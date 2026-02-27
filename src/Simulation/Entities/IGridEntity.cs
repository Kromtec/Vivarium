namespace Vivarium.Entities;

public interface IGridEntity
{
    long Id { get; set; }
    int Index { get; set; }
    int X { get; }
    int Y { get; }
}