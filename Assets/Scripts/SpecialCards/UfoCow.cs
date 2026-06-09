public class UfoCow : SpecialCardScriptBase
{
    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return unitCardData is UfoCowCardData;
    }

    public override int GetAttackRange(Unit unit, CharacterCardData unitCardData)
    {
        return 0;
    }

    public override void OnAfterSpawn(Unit unit, CharacterCardData unitCardData)
    {
        ExecuteAutoFieldRoutine(unit, refreshTurnStartTile: true);
    }

    public override void OnOwnerTurnStart(Unit unit, CharacterCardData unitCardData)
    {
        ExecuteAutoFieldRoutine(unit, refreshTurnStartTile: true);
    }

    public bool CanConsumeEnemyField(HexTile tile, string unitOwner)
    {
        return tile != null
            && tile.HasWorldEffect()
            && tile.isFieldTile
            && tile.worldEffectOwner != "none"
            && tile.worldEffectOwner != unitOwner;
    }

    public bool ConsumeOneFieldStep(Unit ufoCow, HexGrid grid, int consumeAmount = -1)
    {
        if (ufoCow == null || grid == null || ufoCow.currentTile == null)
        {
            return false;
        }

        WorldEffectManager worldEffectManager = UnityEngine.Object.FindFirstObjectByType<WorldEffectManager>();
        if (worldEffectManager == null)
        {
            return false;
        }

        HexTile currentTile = ufoCow.currentTile;

        if (!CanConsumeEnemyField(currentTile, ufoCow.owner))
        {
            return false;
        }
        int configuredConsumeAmount = 1;
        if (ufoCow.sourceCharacterCardData is UfoCowCardData ufoCowCardData)
        {
            configuredConsumeAmount = UnityEngine.Mathf.Max(1, ufoCowCardData.fieldConsumeAmount);
        }
        int resolvedConsumeAmount = consumeAmount > 0 ? consumeAmount : configuredConsumeAmount;
        int safeConsume = UnityEngine.Mathf.Max(1, resolvedConsumeAmount);

        if (worldEffectManager.TryDamageField(currentTile, safeConsume))
        {
            UnityEngine.Debug.Log($"[SpecialTrigger][UfoCow] Consumed field tile at ({currentTile.coord.q},{currentTile.coord.r}).");
            return true;
        }

        return false;
    }

    public override void OnAfterMove(Unit unit, CharacterCardData unitCardData, HexTile destinationTile)
    {
        ExecuteAutoFieldRoutine(unit, refreshTurnStartTile: false);
    }

    private void ExecuteAutoFieldRoutine(Unit unit, bool refreshTurnStartTile)
    {
        if (unit == null || unit.currentTile == null)
        {
            return;
        }

        HexGrid grid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();
        if (grid == null)
        {
            return;
        }

        bool consumed = ConsumeOneFieldStep(unit, grid);
        
        if (!consumed)
        {
            HexTile target = FindClosestEnemyField(unit);
            if (target != null)
            {
                // We no longer check if the engine thinks the tile can be occupied.
                // We are taking its spot!
                unit.currentTile.ClearUnitOccupant();
                unit.PlaceOnTile(target);
                if (refreshTurnStartTile)
                {
                    unit.turnStartTile = target;
                }
                
                int amount = 1;
                if (unit.sourceCharacterCardData is UfoCowCardData cd)
                {
                    amount = UnityEngine.Mathf.Max(1, cd.fieldConsumeAmount);
                }
                
                WorldEffectManager wem = UnityEngine.Object.FindFirstObjectByType<WorldEffectManager>();
                if (wem != null)
                {
                    wem.TryDamageField(target, amount);
                    consumed = true;
                    UnityEngine.Debug.Log($"[SpecialTrigger][UfoCow] Jumped to and zapped field at {target.coord.q},{target.coord.r}");
                }
            }
        }

        if (consumed)
        {
            unit.MarkMoved(unit.GetEffectiveMoveRange());
            unit.MarkAttacked();
        }
        else
        {
            UnityEngine.Debug.Log("[SpecialTrigger][UfoCow] No enemy field tile consumed.");
        }
    }

    private HexTile FindClosestEnemyField(Unit unit)
    {
        HexTile[] allTiles = UnityEngine.Object.FindObjectsByType<HexTile>(UnityEngine.FindObjectsSortMode.None);
        System.Collections.Generic.List<HexTile> validTiles = new System.Collections.Generic.List<HexTile>();

        for (int i = 0; i < allTiles.Length; i++)
        {
            HexTile tile = allTiles[i];
            
            if (tile.HasWorldEffect() && tile.isFieldTile && tile.worldEffectOwner != unit.owner && tile.worldEffectOwner != "none")
            {
                validTiles.Add(tile);
            }
        }

        if (validTiles.Count == 0)
        {
            return null;
        }

        validTiles.Sort((a, b) => HexUtils.GetHexDistance(unit.currentTile, a).CompareTo(HexUtils.GetHexDistance(unit.currentTile, b)));
        return validTiles[0];
    }
}
