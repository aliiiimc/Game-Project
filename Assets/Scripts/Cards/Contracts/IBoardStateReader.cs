// Read-only interface for querying board state; consumed by validators and effects to check tile validity and occupancy without mutation.
public interface IBoardStateReader
{
    bool IsTileValid(AxialCoord tile);
    bool IsTileOccupied(AxialCoord tile);
    CardRuntimeState GetCardAt(AxialCoord tile);
}
