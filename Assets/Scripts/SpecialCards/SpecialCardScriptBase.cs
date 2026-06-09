using UnityEngine;

public abstract class SpecialCardScriptBase : ISpecialCardScript
{
    public abstract bool IsMatch(Unit unit, CharacterCardData unitCardData);

    public virtual int GetAttackRange(Unit unit, CharacterCardData unitCardData)
    {
        return unit != null ? unit.attackRange : 0;
    }

    public virtual AttackType GetAttackType(Unit unit, CharacterCardData unitCardData)
    {
        return unitCardData != null ? unitCardData.attackType : AttackType.Melee;
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

    public virtual void OnAfterSpawn(Unit unit, CharacterCardData unitCardData)
    {
    }

    public virtual void OnOwnerTurnStart(Unit unit, CharacterCardData unitCardData)
    {
    }

    protected static bool CardNameMatches(CharacterCardData cardData, string expected)
    {
        if (cardData == null || string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        return cardData.MatchesSpecialCard(string.Empty, expected);
    }

    protected static bool CardMatches(CharacterCardData cardData, string expectedSpecialId, string fallbackDisplayName)
    {
        if (cardData == null)
        {
            return false;
        }

        return cardData.MatchesSpecialCard(expectedSpecialId, fallbackDisplayName);
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

    public static void PlayProjectileFromUnit(Unit attacker, CharacterCardData attackerCardData, HexTile targetTile)
    {
        if (attacker == null || attackerCardData == null || targetTile == null)
        {
            return;
        }

        ProjectileVisualSettings visuals = GetProjectileVisualSettings(attackerCardData);
        if (visuals == null || visuals.projectilePrefab == null)
        {
            return;
        }

        ProjectileVisual.Spawn(visuals, attacker.transform.position, targetTile.transform.position);
    }

    public static void PlayProjectileFromWorldEffect(WorldEffect worldEffect, HexTile targetTile)
    {
        if (worldEffect == null || worldEffect.sourceCard == null || targetTile == null)
        {
            return;
        }

        ProjectileVisualSettings visuals = GetProjectileVisualSettings(worldEffect.sourceCard.SourceCard);
        if (visuals == null || visuals.projectilePrefab == null)
        {
            return;
        }

        ProjectileVisual.Spawn(visuals, worldEffect.transform.position, targetTile.transform.position);
    }

    public static ProjectileVisualSettings GetProjectileVisualSettings(CardData cardData)
    {
        return cardData switch
        {
            ArcherCardData archerCardData => archerCardData.projectileVisuals,
            DragonCardData dragonCardData => dragonCardData.projectileVisuals,
            WatchTowerCardData watchTowerCardData => watchTowerCardData.projectileVisuals,
            AntiAirTowerCardData antiAirTowerCardData => antiAirTowerCardData.projectileVisuals,
            BomberCardData bomberCardData => bomberCardData.projectileVisuals,
            _ => null
        };
    }
}
