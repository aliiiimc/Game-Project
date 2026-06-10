using UnityEngine;

public class Engineer : SpecialCardScriptBase
{
    private const int DefaultStructureRepairBoostAmount = 6;

    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return unitCardData is EngineerCardData;
    }

    public override bool CanTarget(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        return GetAttackType(attackerCardData) == AttackType.HealFix
            && TargetsGround(attackerCardData)
            && TryGetRepairableStructure(tile, activeOwner, out _, out _, out _);
    }

    public override bool TryHandleAttack(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        if (!TryGetRepairableStructure(tile, activeOwner, out WorldEffect worldEffect, out int currentHp, out int maxHp))
        {
            return false;
        }

        int repairBoostAmount = GetRepairBoostAmount(attackerCardData);
        if (repairBoostAmount <= 0)
        {
            return false;
        }

        int missingHp = Mathf.Max(0, maxHp - currentHp);
        int restoredHp = Mathf.Min(repairBoostAmount, missingHp);
        if (restoredHp <= 0)
        {
            return false;
        }

        worldEffect.sourceCard.ApplyHeal(restoredHp);
        if (worldEffect.sourceCard.CurrentHp.HasValue)
        {
            worldEffect.health = Mathf.Clamp(worldEffect.sourceCard.CurrentHp.Value, 0, maxHp);
        }
        else
        {
            worldEffect.health = Mathf.Clamp(worldEffect.health + restoredHp, 0, maxHp);
        }

        Debug.Log($"[SpecialTrigger][Engineer] Repaired allied structure at ({tile.coord.q},{tile.coord.r}) for {restoredHp} HP.");
        FortGame.UI.HUDManager hudManager = Object.FindFirstObjectByType<FortGame.UI.HUDManager>();
        if (hudManager != null)
        {
            string structureName = worldEffect?.sourceCard?.SourceCard?.DisplayName ?? "structure";
            hudManager.ShowSpellAnnouncement($"Engineer repaired {structureName} for {restoredHp} HP.");
        }
        return true;
    }

    private static int GetRepairBoostAmount(CharacterCardData attackerCardData)
    {
        int repairBoostAmount = DefaultStructureRepairBoostAmount;
        if (attackerCardData is EngineerCardData engineerCardData)
        {
            repairBoostAmount = Mathf.Max(0, engineerCardData.structureRepairBoostAmount);
        }

        return repairBoostAmount;
    }

    private static bool TryGetRepairableStructure(HexTile tile, string activeOwner, out WorldEffect worldEffect, out int currentHp, out int maxHp)
    {
        worldEffect = null;
        currentHp = 0;
        maxHp = 0;

        if (tile == null
            || string.IsNullOrWhiteSpace(activeOwner)
            || !tile.HasWorldEffect()
            || tile.worldEffectOwner != activeOwner)
        {
            return false;
        }

        WorldEffectManager worldEffectManager = Object.FindFirstObjectByType<WorldEffectManager>();
        if (worldEffectManager == null)
        {
            return false;
        }

        worldEffect = worldEffectManager.FindWorldEffectOnTile(tile);
        if (worldEffect == null
            || worldEffect.sourceCard == null
            || !(worldEffect.sourceCard.SourceCard is WorldEffectCardData worldEffectCard)
            || worldEffectCard.category != WorldEffectCategory.Structure
            || !worldEffectCard.structureHp.HasValue
            || !worldEffect.sourceCard.CurrentHp.HasValue)
        {
            return false;
        }

        maxHp = Mathf.Max(0, worldEffectCard.structureHp.Value);
        currentHp = Mathf.Clamp(worldEffect.sourceCard.CurrentHp.Value, 0, maxHp);
        return maxHp > 0 && currentHp < maxHp;
    }
}
