using System.Collections.Generic;
using UnityEngine;

public static class HexUtils
{
    private static readonly AxialCoord[] directions = new AxialCoord[]
    {
        new AxialCoord(+1,  0),
        new AxialCoord(-1,  0),
        new AxialCoord(+1, -1),
        new AxialCoord( 0, -1),
        new AxialCoord(-1, +1),
        new AxialCoord( 0, +1),
    };

    public static List<HexTile> GetNeighbors(HexTile tile, HexGrid grid)
    {
        List<HexTile> neighbors = new List<HexTile>();
        foreach (AxialCoord dir in directions)
        {
            AxialCoord neighborCoord = new AxialCoord(tile.coord.q + dir.q, tile.coord.r + dir.r);
            HexTile neighbor = grid.GetTile(neighborCoord);
            if (neighbor != null)
                neighbors.Add(neighbor);
        }
        return neighbors;
    }

    public static List<HexTile> GetTilesInRange(HexTile start, int range, HexGrid grid)
    {
        List<HexTile> visited = new List<HexTile>();
        List<HexTile> frontier = new List<HexTile>();

        visited.Add(start);
        frontier.Add(start);

        for (int step = 0; step < range; step++)
        {
            List<HexTile> nextFrontier = new List<HexTile>();
            foreach (HexTile tile in frontier)
            {
                foreach (HexTile neighbor in GetNeighbors(tile, grid))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        nextFrontier.Add(neighbor);
                    }
                }
            }
            frontier = nextFrontier;
        }

        visited.Remove(start);
        return visited;
    }
}
