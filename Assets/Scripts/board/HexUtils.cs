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

    public static int GetHexDistance(HexTile start, HexTile target)
    {
        if (start == null || target == null)
        {
            return -1;
        }

        return GetAxialDistance(start.coord, target.coord);
    }

    public static int GetAxialDistance(AxialCoord start, AxialCoord target)
    {
        int dq = start.q - target.q;
        int dr = start.r - target.r;
        int ds = -dq - dr;

        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
    }

    public static List<HexTile> GetReachableMoveTiles(HexTile start, int range, HexGrid grid, MovementType movementType = MovementType.Ground)
    {
        List<HexTile> reachableTiles = new List<HexTile>();
        if (start == null || grid == null || range <= 0)
        {
            return reachableTiles;
        }

        List<HexTile> visited = new List<HexTile>();
        Queue<HexTile> tileQueue = new Queue<HexTile>();
        Queue<int> distanceQueue = new Queue<int>();

        visited.Add(start);
        tileQueue.Enqueue(start);
        distanceQueue.Enqueue(0);

        while (tileQueue.Count > 0)
        {
            HexTile current = tileQueue.Dequeue();
            int distance = distanceQueue.Dequeue();

            if (distance >= range)
            {
                continue;
            }

            foreach (HexTile neighbor in GetNeighbors(current, grid))
            {
                if (visited.Contains(neighbor))
                {
                    continue;
                }

                visited.Add(neighbor);

                bool canLandOnNeighbor = neighbor.IsEmpty();
                if (canLandOnNeighbor)
                {
                    reachableTiles.Add(neighbor);
                }

                if (!CanTraverseTile(neighbor, movementType))
                {
                    continue;
                }

                tileQueue.Enqueue(neighbor);
                distanceQueue.Enqueue(distance + 1);
            }
        }

        return reachableTiles;
    }

    public static int GetMoveDistance(HexTile start, HexTile target, HexGrid grid, int maxRange, MovementType movementType = MovementType.Ground)
    {
        if (start == null || target == null || grid == null || maxRange < 0)
        {
            return -1;
        }

        if (start == target)
        {
            return 0;
        }

        List<HexTile> visited = new List<HexTile>();
        Queue<HexTile> tileQueue = new Queue<HexTile>();
        Queue<int> distanceQueue = new Queue<int>();

        visited.Add(start);
        tileQueue.Enqueue(start);
        distanceQueue.Enqueue(0);

        while (tileQueue.Count > 0)
        {
            HexTile current = tileQueue.Dequeue();
            int distance = distanceQueue.Dequeue();

            if (distance >= maxRange)
            {
                continue;
            }

            foreach (HexTile neighbor in GetNeighbors(current, grid))
            {
                if (visited.Contains(neighbor) || !CanTraverseTile(neighbor, movementType, target))
                {
                    continue;
                }

                int nextDistance = distance + 1;
                if (neighbor == target)
                {
                    return nextDistance;
                }

                visited.Add(neighbor);
                tileQueue.Enqueue(neighbor);
                distanceQueue.Enqueue(nextDistance);
            }
        }

        return -1;
    }

    private static bool CanTraverseTile(HexTile tile, MovementType movementType, HexTile target = null)
    {
        if (tile == null)
        {
            return false;
        }

        if (tile == target)
        {
            return true;
        }

        if (movementType == MovementType.Flying)
        {
            return true;
        }

        return tile.IsEmpty();
    }
}
