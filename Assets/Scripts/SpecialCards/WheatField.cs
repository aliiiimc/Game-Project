using System;
using System.Collections.Generic;
using UnityEngine;

public class WheatField
{
    public int GetBonusMoneyPerTurn(WheatFieldCardData worldEffectCard)
    {
        if (worldEffectCard == null)
        {
            return 1;
        }

        return Mathf.Max(0, worldEffectCard.bonusMoneyPerTurn);
    }

    public string CreateClusterId()
    {
        return Guid.NewGuid().ToString("N");
    }

    public List<HexTile> BuildFieldTiles(HexGrid grid, HexTile originTile, int requestedTileCount = -1)
    {
        List<HexTile> fieldTiles = new List<HexTile>();
        if (grid == null || originTile == null)
        {
            return fieldTiles;
        }

        int targetCount = Mathf.Max(1, requestedTileCount);

        // Predefined tight 'square' (rhombus/block) offsets to ensure no dead-ends (horseshoe shapes)
        List<AxialCoord> squareOffsets = new List<AxialCoord>
        {
            new AxialCoord(0, 0),
            new AxialCoord(1, 0),
            new AxialCoord(0, 1),
            new AxialCoord(1, 1),
            new AxialCoord(-1, 1),
            new AxialCoord(0, 2),
            new AxialCoord(1, -1),
            new AxialCoord(-1, 0)
        };

        foreach (AxialCoord offset in squareOffsets)
        {
            if (fieldTiles.Count >= targetCount) break;

            AxialCoord targetCoord = new AxialCoord(originTile.coord.q + offset.q, originTile.coord.r + offset.r);
            HexTile tile = grid.GetTile(targetCoord);

            if (tile != null && (tile.IsEmpty() || tile.HasWorldEffect()))
            {
                if (!fieldTiles.Contains(tile))
                {
                    fieldTiles.Add(tile);
                }
            }
        }

        // If the tight block couldn't find enough tiles (e.g. near map edges), fallback to BFS to fill the rest
        if (fieldTiles.Count < targetCount)
        {
            Queue<HexTile> frontier = new Queue<HexTile>();
            HashSet<HexTile> visited = new HashSet<HexTile>();
            
            foreach (HexTile ft in fieldTiles)
            {
                frontier.Enqueue(ft);
                visited.Add(ft);
            }

            if (frontier.Count == 0)
            {
                frontier.Enqueue(originTile);
                visited.Add(originTile);
            }

            while (frontier.Count > 0 && fieldTiles.Count < targetCount)
            {
                HexTile current = frontier.Dequeue();

                if (!fieldTiles.Contains(current) && (current.IsEmpty() || current.HasWorldEffect()))
                {
                    fieldTiles.Add(current);
                }

                if (fieldTiles.Count >= targetCount) break;

                List<HexTile> neighbors = HexUtils.GetNeighbors(current, grid);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    HexTile neighbor = neighbors[i];
                    if (neighbor == null || visited.Contains(neighbor)) continue;

                    visited.Add(neighbor);
                    frontier.Enqueue(neighbor);
                }
            }
        }

        return fieldTiles;
    }

    public bool ApplyFieldCluster(HexGrid grid, HexTile originTile, string owner, Sprite fieldSprite, CardRuntimeState sourceCard, out string clusterId, int tileCount = -1, int hpPerTile = -1)
    {
        clusterId = string.Empty;
        if (grid == null || originTile == null || string.IsNullOrWhiteSpace(owner) || sourceCard == null)
        {
            return false;
        }

        WorldEffectManager worldEffectManager = UnityEngine.Object.FindFirstObjectByType<WorldEffectManager>();
        if (worldEffectManager == null)
        {
            return false;
        }

        WheatFieldCardData worldEffectCard = sourceCard.SourceCard as WheatFieldCardData;
        int configuredTileCount = worldEffectCard != null ? Mathf.Max(1, worldEffectCard.tilesPerField) : 6;
        int configuredHpPerTile = worldEffectCard != null ? Mathf.Max(1, worldEffectCard.hpPerTile) : 1;
        int resolvedTileCount = tileCount > 0 ? tileCount : configuredTileCount;
        int resolvedHpPerTile = hpPerTile > 0 ? hpPerTile : configuredHpPerTile;
        int bonusMoneyPerTurn = GetBonusMoneyPerTurn(worldEffectCard);

        List<HexTile> tiles = BuildFieldTiles(grid, originTile, resolvedTileCount);
        if (tiles.Count == 0)
        {
            return false;
        }

        clusterId = CreateClusterId();
        int safeHpPerTile = Mathf.Max(1, resolvedHpPerTile);
        int placedTileCount = 0;
        List<HexTile> placedTiles = new List<HexTile>();

        for (int i = 0; i < tiles.Count; i++)
        {
            HexTile tile = tiles[i];
            bool placed = false;
            if (tile.IsEmpty())
            {
                placed = worldEffectManager.TryPlaceFromCard(tile, owner, sourceCard, out _);
            }
            else if (tile.HasWorldEffect() && !tile.HasUnitOccupant())
            {
                bool ownershipReady = tile.worldEffectOwner == owner || worldEffectManager.TryColonize(tile, owner);
                placed = ownershipReady && worldEffectManager.TryReplace(tile, owner, sourceCard, out _);
            }

            if (!placed)
            {
                continue;
            }

            placedTileCount++;
            placedTiles.Add(tile);
        }

        // Use structureHp directly as the global HP pool of the field cluster, bypassing the obsolete hpPerTile calculation
        int clusterTotalHp = worldEffectCard != null && worldEffectCard.structureHp.HasValue 
            ? worldEffectCard.structureHp.Value 
            : (placedTileCount * safeHpPerTile);

        for (int i = 0; i < placedTiles.Count; i++)
        {
            if (worldEffectManager.TrySetFieldData(placedTiles[i], clusterId, clusterTotalHp, bonusMoneyPerTurn))
            {
                continue;
            }
        }

        Debug.Log($"[SpecialTrigger][WheatField] Cluster '{clusterId}' created with {placedTileCount} tile(s) for owner '{owner}'.");

        return placedTileCount > 0;
    }
}
