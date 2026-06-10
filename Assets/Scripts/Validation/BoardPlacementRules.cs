using UnityEngine;

public static class BoardPlacementRules // verify if a board-placement card can be put on the board.
{
    public static bool CanPlaceCharacter(AxialCoord coord, string playerKey, HexGrid grid, CharacterCardData characterCard = null)
    {
        if (grid == null)
        {
            return false;
        }

        HexTile tile = grid.GetTile(coord);
        if (tile == null)
        {
            return false;
        }

        // (abdo :) Movement may allow standing on some world effects, but normal card spawns still need a plain empty tile.
        if (tile.HasUnitOccupant() || tile.tileType == "fort" || tile.HasWorldEffect() || !tile.CanUnitOccupy())
        {
            return false;
        }

        if (CanPlaceUfoCowAdjacentToEnemyField(characterCard, tile, playerKey, grid))
        {
            return true;
        }

        if (grid.IsInPlayerDeploymentZone(coord, playerKey))
        {
            return true;
        }

        // Camp special rule: when active, it opens a new spawn location around owned camp tiles.
        Camp camp = new Camp();
        CampCardData campCardData = ResolveCampCardData();
        if (!camp.ForcesNewSpawnLocation(campCardData))
        {
            return false;
        }

        return camp.CanOpenSpawnLocation(tile, playerKey, grid, campCardData);
    }

    // Ali: keep World Effect placement in one shared helper so player and AI validation cannot drift.
    public static bool CanPlaceWorldEffect(AxialCoord coord, string playerKey, HexGrid grid)
    {
        if (grid == null)
        {
            return false;
        }

        HexTile tile = grid.GetTile(coord);
        if (tile == null)
        {
            return false;
        }

        if (!tile.CanPlaceWorldEffect())
        {
            return false;
        }

        return grid.IsInPlayerHalf(coord, playerKey);
    }

    private static CampCardData ResolveCampCardData()
    {
        CardLibrary[] libraries = Object.FindObjectsByType<CardLibrary>(FindObjectsSortMode.None);
        for (int i = 0; i < libraries.Length; i++)
        {
            CardLibrary library = libraries[i];
            if (library == null || library.cards == null)
            {
                continue;
            }

            for (int j = 0; j < library.cards.Count; j++)
            {
                if (!(library.cards[j] is CampCardData campCardData))
                {
                    continue;
                }

                return campCardData;
            }
        }

        return null;
    }

    private static bool CanPlaceUfoCowAdjacentToEnemyField(CharacterCardData characterCard, HexTile tile, string playerKey, HexGrid grid)
    {
        if (!(characterCard is UfoCowCardData) || tile == null || grid == null || string.IsNullOrWhiteSpace(playerKey))
        {
            return false;
        }

        System.Collections.Generic.List<HexTile> neighbors = HexUtils.GetNeighbors(tile, grid);
        for (int i = 0; i < neighbors.Count; i++)
        {
            HexTile neighbor = neighbors[i];
            if (neighbor == null
                || !neighbor.HasWorldEffect()
                || !neighbor.isFieldTile
                || neighbor.worldEffectOwner == "none"
                || neighbor.worldEffectOwner == playerKey)
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
