using UnityEngine;

public abstract class SpecialCardScriptBase : ISpecialCardScript
{
    public abstract bool IsMatch(Unit unit, CharacterCardData unitCardData);

    public virtual int GetAttackRange(Unit unit, CharacterCardData unitCardData)
    {
        return unit != null ? unit.attackRange : 0;
    }

    public virtual bool CanTarget(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        return false;
    }

    public virtual bool TryHandleAttack(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        return false;
    }

    public virtual bool ConsumeMoveAction(Unit unit, CharacterCardData unitCardData)
    {
        return true;
    }

    public virtual void OnBeforeMove(Unit unit, CharacterCardData unitCardData)
    {
    }

    public virtual void OnAfterMove(Unit unit, CharacterCardData unitCardData, HexTile destinationTile)
    {
    }

    protected static bool CardNameMatches(CharacterCardData cardData, string expected)
    {
        if (cardData == null || string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        return cardData.DisplayName.Trim().ToLowerInvariant() == expected.Trim().ToLowerInvariant();
    }

    protected static AttackType GetAttackType(CharacterCardData cardData)
    {
        return cardData != null ? cardData.attackType : AttackType.Melee;
    }

    protected static AttackTarget GetAttackTarget(CharacterCardData cardData)
    {
        return cardData != null ? cardData.attackTarget : AttackTarget.Ground;
    }

    protected static MovementType GetMovementType(CharacterCardData cardData)
    {
        return cardData != null ? cardData.movementType : MovementType.Ground;
    }

    protected static bool TargetsGround(CharacterCardData cardData)
    {
        AttackTarget attackTarget = GetAttackTarget(cardData);
        return attackTarget == AttackTarget.Ground || attackTarget == AttackTarget.Both;
    }

    protected static bool CanAttackEnemyTileWithProfile(CharacterCardData cardData, HexTile tile)
    {
        if (tile == null || GetAttackType(cardData) == AttackType.HealFix)
        {
            return false;
        }

        bool targetIsAir = false;
        if (tile.tileType == "unit")
        {
            Unit targetUnit = FindUnitOnTile(tile);
            targetIsAir = targetUnit != null && GetMovementType(targetUnit.sourceCharacterCardData) == MovementType.Flying;
        }

        AttackTarget attackTarget = GetAttackTarget(cardData);
        if (attackTarget == AttackTarget.Both)
        {
            return true;
        }

        if (targetIsAir)
        {
            return attackTarget == AttackTarget.Air;
        }

        return attackTarget == AttackTarget.Ground;
    }

    protected static Unit FindUnitOnTile(HexTile tile)
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
