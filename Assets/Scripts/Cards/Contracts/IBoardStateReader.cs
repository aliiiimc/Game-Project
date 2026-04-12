// Read-only board API consumed by validators/effects.
public interface IBoardStateReader
{
    bool IsTileValid(AxialCoord tile);
    bool IsTileOccupied(AxialCoord tile);
    CardRuntimeState GetCardAt(AxialCoord tile);
}
