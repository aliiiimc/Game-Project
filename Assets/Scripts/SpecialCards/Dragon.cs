using UnityEngine;

public class Dragon : SpecialCardScriptBase
{
    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return unitCardData is DragonCardData
            || CardMatches(unitCardData, SpecialCardIds.CharacterDragon, "Dragon");
    }

    public override int GetAttackRange(Unit unit, CharacterCardData unitCardData)
    {
        if (unit == null)
        {
            return 0;
        }

        return unitCardData != null
            ? UnityEngine.Mathf.Max(0, unitCardData.attackRange)
            : (unit != null ? UnityEngine.Mathf.Max(0, unit.attackRange) : 0);
    }

    public override bool CanTarget(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        return tile != null
            && tile.HasWorldEffect()
            && tile.worldEffectOwner != "none"
            && tile.worldEffectOwner != activeOwner
            && CanAttackEnemyTileWithProfile(attackerCardData, tile);
    }

    public override bool TryHandleAttack(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        if (attacker == null || !CanTarget(attacker, attackerCardData, tile, activeOwner))
        {
            return false;
        }

        PlayProjectileFromUnit(attacker, attackerCardData, tile);
        int dealtDamage;
        bool attackApplied = TryDamageWorldEffect(tile, Mathf.Max(0, attacker.attack), out dealtDamage);
        if (attackApplied)
        {
            Debug.Log($"[SpecialTrigger][Dragon] Burned world effect at ({tile.coord.q},{tile.coord.r}) for {dealtDamage} damage.");
        }

        return attackApplied;
    }

    private static bool TryDamageWorldEffect(HexTile tile, int damage, out int dealtDamage)
    {
        dealtDamage = 0;

        if (tile == null || !tile.HasWorldEffect() || damage <= 0)
        {
            return false;
        }

        WorldEffectManager worldEffectManager = Object.FindFirstObjectByType<WorldEffectManager>();
        if (worldEffectManager == null)
        {
            return false;
        }

        if (tile.isFieldTile)
        {
            int fieldHpBefore = Mathf.Max(0, tile.fieldHp);
            bool damaged = worldEffectManager.TryDamageField(tile, damage);
            if (!damaged)
            {
                return false;
            }

            int fieldHpAfter = tile.HasWorldEffect() && tile.isFieldTile
                ? Mathf.Max(0, tile.fieldHp)
                : 0;
            dealtDamage = Mathf.Max(0, fieldHpBefore - fieldHpAfter);
            return true;
        }

        WorldEffect worldEffect = worldEffectManager.FindWorldEffectOnTile(tile);
        if (worldEffect == null)
        {
            return worldEffectManager.Remove(tile);
        }

        if (worldEffect.sourceCard != null)
        {
            int hpBefore = worldEffect.sourceCard.CurrentHp.HasValue
                ? Mathf.Max(0, worldEffect.sourceCard.CurrentHp.Value)
                : Mathf.Max(0, worldEffect.health);
            worldEffect.sourceCard.ApplyDamage(damage);
            if (worldEffect.sourceCard.CurrentHp.HasValue)
            {
                worldEffect.health = worldEffect.sourceCard.CurrentHp.Value;
            }
            else
            {
                worldEffect.health = Mathf.Max(0, worldEffect.health - damage);
            }

            dealtDamage = Mathf.Max(0, hpBefore - Mathf.Max(0, worldEffect.health));
        }
        else
        {
            int hpBefore = Mathf.Max(0, worldEffect.health);
            worldEffect.health = Mathf.Max(0, worldEffect.health - damage);
            dealtDamage = Mathf.Max(0, hpBefore - Mathf.Max(0, worldEffect.health));
        }

        if (worldEffect.health <= 0)
        {
            return worldEffectManager.Remove(tile);
        }

        return true;
    }
}
