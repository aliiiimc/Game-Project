using System.Collections.Generic;
using UnityEngine;

public class Wall
{
    public int GetTilesPerWall(WallCardData worldEffectCard)
    {
        if (worldEffectCard == null)
        {
            return 3;
        }

        return Mathf.Max(1, worldEffectCard.tilesPerWall);
    }

    public List<HexTile> BuildWallTiles(HexGrid grid, HexTile originTile, int requestedTileCount = -1)
    {
        List<HexTile> wallTiles = new List<HexTile>();
        if (grid == null || originTile == null)
        {
            return wallTiles;
        }

        int targetCount = Mathf.Max(1, requestedTileCount);
        int originColumn = grid.AxialToOffsetColumn(originTile.coord);
        int originRow = originTile.coord.r;

        for (int rowOffset = 0; rowOffset < targetCount; rowOffset++)
        {
            AxialCoord coord = HexGrid.OffsetToAxial(originColumn, originRow + rowOffset);
            HexTile tile = grid.GetTile(coord);
            if (tile != null)
            {
                wallTiles.Add(tile);
            }
        }

        return wallTiles;
    }

    public bool ApplyWallLine(HexGrid grid, HexTile originTile, string owner, CardRuntimeState sourceCard, int tileCount = -1)
    {
        if (grid == null || originTile == null || string.IsNullOrWhiteSpace(owner) || sourceCard == null)
        {
            return false;
        }

        WorldEffectManager worldEffectManager = Object.FindFirstObjectByType<WorldEffectManager>();
        if (worldEffectManager == null)
        {
            return false;
        }

        WallCardData worldEffectCard = sourceCard.SourceCard as WallCardData;
        int resolvedTileCount = tileCount > 0 ? tileCount : GetTilesPerWall(worldEffectCard);
        List<HexTile> tiles = BuildWallTiles(grid, originTile, resolvedTileCount);
        if (tiles.Count == 0)
        {
            return false;
        }

        int placedTileCount = 0;
        for (int i = 0; i < tiles.Count; i++)
        {
            HexTile tile = tiles[i];
            bool placed = false;
            if (tile.IsEmpty())
            {
                placed = worldEffectManager.TryPlaceFromCard(tile, owner, sourceCard, out _);
            }

            if (placed)
            {
                placedTileCount++;
            }
        }

        Debug.Log($"[SpecialTrigger][Wall] Wall line created with {placedTileCount} tile(s) for owner '{owner}'.");
        return placedTileCount > 0;
    }
}
