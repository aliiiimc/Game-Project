public interface IBoardStateReader
{
    bool IsTileValid(AxialCoord tile);
    bool IsTileOccupied(AxialCoord tile);
    CardRuntimeState GetCardAt(AxialCoord tile);
}
