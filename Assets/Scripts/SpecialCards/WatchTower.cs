using System.Collections.Generic;
using UnityEngine;

public class WatchTower
{
    public int ApplyAutomaticAttacks(string threatenedOwnerKey, HexGrid grid, WorldEffectManager worldEffectManager)
    {
        if (string.IsNullOrWhiteSpace(threatenedOwnerKey) || grid == null || worldEffectManager == null)
        {
            return 0;
        }

        WorldEffect[] allWorldEffects = Object.FindObjectsByType<WorldEffect>(FindObjectsSortMode.None);
        int totalHits = 0;
        SpellManager spellManager = SpellManager.GetOrCreate();

        for (int i = 0; i < allWorldEffects.Length; i++)
        {
            WorldEffect worldEffect = allWorldEffects[i];
            if (!IsWatchTower(worldEffect) || worldEffect.currentTile == null)
            {
                continue;
            }

            if (spellManager != null && spellManager.IsWorldEffectDisabled(worldEffect))
            {
                continue;
            }

            if (worldEffect.owner == threatenedOwnerKey)
            {
                continue;
            }

            int damage = GetDamage(worldEffect);
            int range = GetAttackRange(worldEffect);
            if (damage <= 0 || range <= 0)
            {
                continue;
            }

            List<HexTile> tilesInRange = HexUtils.GetTilesInRange(worldEffect.currentTile, range, grid);
            for (int j = 0; j < tilesInRange.Count; j++)
            {
                HexTile tile = tilesInRange[j];
                Unit targetUnit = FindUnitOnTile(tile);
                if (targetUnit == null || targetUnit.owner != threatenedOwnerKey)
                {
                    continue;
                }

                bool targetIsAir = targetUnit.sourceCharacterCardData != null
                    && targetUnit.sourceCharacterCardData.movementType == MovementType.Flying;
                if (!worldEffectManager.CanTargetWithProfile(worldEffect.currentTile, targetIsAir))
                {
                    continue;
                }

                SpecialCardScriptBase.PlayProjectileFromWorldEffect(worldEffect, tile);
                targetUnit.ApplyDamage(damage);
                totalHits++;
                Debug.Log($"[SpecialTrigger][WatchTower] Hit unit at ({tile.coord.q},{tile.coord.r}) for {damage} damage.");
            }
        }

        return totalHits;
    }

    private static bool IsWatchTower(WorldEffect worldEffect)
    {
        return worldEffect != null
            && worldEffect.sourceCard != null
            && worldEffect.sourceCard.SourceCard != null
            && (worldEffect.sourceCard.SourceCard is WatchTowerCardData
                || worldEffect.sourceCard.SourceCard.MatchesSpecialCard(SpecialCardIds.WorldWatchTower, "Watch Tower"));
    }

    private static int GetDamage(WorldEffect worldEffect)
    {
        if (worldEffect == null || worldEffect.sourceCard == null)
        {
            return 0;
        }

        if (worldEffect.sourceCard.CurrentDamage.HasValue)
        {
            return Mathf.Max(0, worldEffect.sourceCard.CurrentDamage.Value);
        }

        if (worldEffect.sourceCard.SourceCard is WorldEffectCardData worldEffectCard && worldEffectCard.structureDamage.HasValue)
        {
            return Mathf.Max(0, worldEffectCard.structureDamage.Value);
        }

        return 0;
    }

    private static int GetAttackRange(WorldEffect worldEffect)
    {
        if (worldEffect == null || worldEffect.sourceCard == null || !(worldEffect.sourceCard.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return 0;
        }

        int baseRange = 0;
        if (worldEffectCard.worldEffectAttackRange.HasValue)
        {
            baseRange = Mathf.Max(0, worldEffectCard.worldEffectAttackRange.Value);
        }

        return baseRange;
    }

    private static Unit FindUnitOnTile(HexTile tile)
    {
        if (tile == null)
        {
            return null;
        }

        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        for (int i = 0; i < allUnits.Length; i++)
        {
            Unit unit = allUnits[i];
            if (unit != null && unit.currentTile == tile)
            {
                return unit;
            }
        }

        return null;
    }
}
