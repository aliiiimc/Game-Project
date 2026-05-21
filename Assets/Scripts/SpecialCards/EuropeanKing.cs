public class EuropeanKing : SpecialCardScriptBase
{
    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return CardNameMatches(unitCardData, "European King");
    }

    public override bool CanTarget(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        return tile != null
            && GetAttackType(attackerCardData) != AttackType.HealFix
            && TargetsGround(attackerCardData)
            && tile.tileType == "worldEffect"
            && tile.owner != "none"
            && tile.owner != activeOwner;
    }

    public override bool TryHandleAttack(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        if (!CanTarget(attacker, attackerCardData, tile, activeOwner))
        {
            return false;
        }

        WorldEffectManager worldEffectManager = UnityEngine.Object.FindFirstObjectByType<WorldEffectManager>();
        if (worldEffectManager == null || !worldEffectManager.TryColonize(tile, activeOwner))
        {
            UnityEngine.Debug.LogWarning("[SpecialTrigger][EuropeanKing] Colonization failed.");
            return false;
        }

        UnityEngine.Debug.Log($"[SpecialTrigger][EuropeanKing] Colonized world effect at ({tile.coord.q},{tile.coord.r}) and sacrificed attacker.");
        attacker?.Die();
        return true;
    }
}
