public class UfoCow : SpecialCardScriptBase
{
    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return unitCardData is UfoCowCardData;
    }

    public override int GetAttackRange(Unit unit, CharacterCardData unitCardData)
    {
        return 1;
    }

    public override bool CanTarget(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        if (!(attackerCardData is UfoCowCardData)
            || attacker == null
            || attacker.currentTile == null
            || tile == null
            || !tile.HasWorldEffect()
            || !tile.isFieldTile
            || tile.worldEffectOwner == "none"
            || tile.worldEffectOwner == activeOwner)
        {
            return false;
        }

        return HexUtils.GetHexDistance(attacker.currentTile, tile) == 1;
    }

    public override bool TryHandleAttack(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        if (!(attackerCardData is UfoCowCardData ufoCowCardData)
            || attacker == null
            || tile == null
            || !CanTarget(attacker, attackerCardData, tile, activeOwner))
        {
            return false;
        }

        WorldEffectManager worldEffectManager = UnityEngine.Object.FindFirstObjectByType<WorldEffectManager>();
        if (worldEffectManager == null)
        {
            return false;
        }

        int safeConsume = UnityEngine.Mathf.Max(1, ufoCowCardData.fieldConsumeAmount);
        bool damaged = worldEffectManager.TryDamageField(tile, safeConsume);
        if (damaged)
        {
            UnityEngine.Debug.Log($"[SpecialTrigger][UfoCow] Damaged field tile at ({tile.coord.q},{tile.coord.r}) for {safeConsume}.");
        }

        return damaged;
    }
}
