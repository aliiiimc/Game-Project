using UnityEngine;

public class Priest : SpecialCardScriptBase
{
    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return CardNameMatches(unitCardData, "Priest");
    }

    public override bool CanTarget(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        return TryGetHealableAllyUnit(tile, activeOwner, out Unit targetUnit)
            && GetAttackType(attackerCardData) == AttackType.HealFix
            && CanTargetUnitWithProfile(attackerCardData, targetUnit);
    }

    public override bool TryHandleAttack(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        if (!TryGetHealableAllyUnit(tile, activeOwner, out Unit targetUnit))
        {
            return false;
        }

        if (GetAttackType(attackerCardData) != AttackType.HealFix || !CanTargetUnitWithProfile(attackerCardData, targetUnit))
        {
            return false;
        }

        int hpBefore = Mathf.Max(0, targetUnit.health);
        targetUnit.ApplyHeal(Mathf.Max(0, attacker.attack));
        int restoredHp = Mathf.Max(0, targetUnit.health - hpBefore);
        if (restoredHp <= 0)
        {
            return false;
        }

        Debug.Log($"[SpecialTrigger][Priest] Healed allied unit at ({tile.coord.q},{tile.coord.r}) for {restoredHp} HP.");
        return true;
    }

    private static bool TryGetHealableAllyUnit(HexTile tile, string activeOwner, out Unit targetUnit)
    {
        targetUnit = null;

        if (tile == null
            || string.IsNullOrWhiteSpace(activeOwner)
            || tile.tileType != "unit"
            || tile.owner != activeOwner)
        {
            return false;
        }

        targetUnit = FindUnitOnTile(tile);
        if (targetUnit == null)
        {
            return false;
        }

        int maxHp = targetUnit.sourceCharacterCardData != null
            ? Mathf.Max(0, targetUnit.sourceCharacterCardData.maxHp)
            : 0;

        return maxHp > 0 && Mathf.Clamp(targetUnit.health, 0, maxHp) < maxHp;
    }

    private static bool CanTargetUnitWithProfile(CharacterCardData cardData, Unit targetUnit)
    {
        if (targetUnit == null)
        {
            return false;
        }

        AttackTarget attackTarget = GetAttackTarget(cardData);
        if (attackTarget == AttackTarget.Both)
        {
            return true;
        }

        bool targetIsAir = GetMovementType(targetUnit.sourceCharacterCardData) == MovementType.Flying;
        if (targetIsAir)
        {
            return attackTarget == AttackTarget.Air;
        }

        return attackTarget == AttackTarget.Ground;
    }
}
