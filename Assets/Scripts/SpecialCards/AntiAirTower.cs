using System.Collections.Generic;
using UnityEngine;

public class AntiAirTower
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
            if (!IsAntiAirTower(worldEffect) || worldEffect.currentTile == null)
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

            Unit targetUnit = FindBestAirTarget(worldEffect.currentTile, threatenedOwnerKey, range, grid, worldEffectManager);
            if (targetUnit == null)
            {
                continue;
            }

            HexTile targetTile = targetUnit.currentTile;
            SpecialCardScriptBase.PlayProjectileFromWorldEffect(worldEffect, targetTile);
            targetUnit.ApplyDamage(damage);
            totalHits++;
            if (targetTile != null)
            {
                Debug.Log(
                    $"[SpecialTrigger][AntiAirTower] Hit air unit at ({targetTile.coord.q},{targetTile.coord.r}) for {damage} damage.");
            }
        }

        return totalHits;
    }

    private static Unit FindBestAirTarget(
        HexTile sourceTile,
        string threatenedOwnerKey,
        int range,
        HexGrid grid,
        WorldEffectManager worldEffectManager)
    {
        if (sourceTile == null || string.IsNullOrWhiteSpace(threatenedOwnerKey) || range <= 0 || grid == null || worldEffectManager == null)
        {
            return null;
        }

        List<HexTile> tilesInRange = HexUtils.GetTilesInRange(sourceTile, range, grid);
        Unit bestTarget = null;
        int bestDistance = int.MaxValue;
        int bestHealth = int.MaxValue;

        for (int i = 0; i < tilesInRange.Count; i++)
        {
            HexTile tile = tilesInRange[i];
            Unit targetUnit = FindUnitOnTile(tile);
            if (targetUnit == null || targetUnit.owner != threatenedOwnerKey || !IsAirUnit(targetUnit))
            {
                continue;
            }

            if (!worldEffectManager.CanTargetWithProfile(sourceTile, targetIsAir: true))
            {
                continue;
            }

            int distance = HexUtils.GetHexDistance(sourceTile, tile);
            int health = Mathf.Max(0, targetUnit.health);
            bool isBetterTarget = bestTarget == null
                || distance < bestDistance
                || (distance == bestDistance && health < bestHealth);

            if (!isBetterTarget)
            {
                continue;
            }

            bestTarget = targetUnit;
            bestDistance = distance;
            bestHealth = health;
        }

        return bestTarget;
    }

    private static bool IsAntiAirTower(WorldEffect worldEffect)
    {
        return worldEffect != null
            && worldEffect.sourceCard != null
            && worldEffect.sourceCard.SourceCard != null
            && (worldEffect.sourceCard.SourceCard is AntiAirTowerCardData
                || worldEffect.sourceCard.SourceCard.MatchesSpecialCard(SpecialCardIds.WorldAntiAirTower, "Anti-Air Tower"));
    }

    private static bool IsAirUnit(Unit unit)
    {
        return unit != null
            && unit.sourceCharacterCardData != null
            && unit.sourceCharacterCardData.movementType == MovementType.Flying;
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
