using UnityEngine;

public class Bomber : SpecialCardScriptBase
{
    private const int BuildingBonusDamage = 8;

    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return CardNameMatches(unitCardData, "Bomber");
    }

    public override bool CanTarget(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        return tile != null
            && tile.owner != "none"
            && tile.owner != activeOwner
            && (tile.tileType == "unit" || tile.tileType == "fort" || tile.tileType == "worldEffect");
    }

    public override bool TryHandleAttack(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        if (attacker == null || !CanTarget(attacker, attackerCardData, tile, activeOwner))
        {
            return false;
        }

        int bombDamage = Mathf.Max(0, attacker.attack + BuildingBonusDamage);
        bool attackApplied = false;

        if (tile.tileType == "unit")
        {
            Unit targetUnit = FindUnitOnTile(tile);
            if (targetUnit != null)
            {
                int targetHpBefore = Mathf.Max(0, targetUnit.health);
                targetUnit.ApplyDamage(attacker.attack);
                int targetHpAfter = Mathf.Max(0, targetUnit.health);
                int dealtDamage = Mathf.Max(0, targetHpBefore - targetHpAfter);
                Debug.Log($"[SpecialTrigger][Bomber] Bomb hit unit at ({tile.coord.q},{tile.coord.r}) for {dealtDamage} damage.");
                attackApplied = true;
            }
        }
        else if (tile.tileType == "fort")
        {
            attackApplied = TryDamageFort(tile, bombDamage);
            if (attackApplied)
            {
                Debug.Log($"[SpecialTrigger][Bomber] Bomb hit fort at ({tile.coord.q},{tile.coord.r}) for {bombDamage} damage.");
            }
        }
        else if (tile.tileType == "worldEffect")
        {
            int dealtDamage;
            attackApplied = TryDamageWorldEffect(tile, bombDamage, out dealtDamage);
            if (attackApplied)
            {
                Debug.Log($"[SpecialTrigger][Bomber] Bomb hit world effect at ({tile.coord.q},{tile.coord.r}) for {dealtDamage} damage.");
            }
        }

        if (!attackApplied)
        {
            return false;
        }

        ApplyBombWeakness(attacker, attackerCardData);
        return true;
    }

    private static bool TryDamageFort(HexTile tile, int damage)
    {
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null || tile == null || damage <= 0)
        {
            return false;
        }

        if (tile.owner == "enemy")
        {
            gameManager.DamagePlayer2Fort(damage);
            return true;
        }

        if (tile.owner == "player")
        {
            gameManager.DamagePlayer1Fort(damage);
            return true;
        }

        return false;
    }

    private static bool TryDamageWorldEffect(HexTile tile, int damage, out int dealtDamage)
    {
        dealtDamage = 0;

        if (tile == null || tile.tileType != "worldEffect" || damage <= 0)
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

            int fieldHpAfter = tile.tileType == "worldEffect" && tile.isFieldTile
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

    private void ApplyBombWeakness(Unit attacker, CharacterCardData attackerCardData)
    {
        if (attacker == null)
        {
            return;
        }

        int damageAfterDroppingBomb = 2;
        if (attackerCardData is BomberCardData bomberCardData)
        {
            damageAfterDroppingBomb = Mathf.Max(0, bomberCardData.damageAfterDroppingBomb);
        }

        int delta = damageAfterDroppingBomb - attacker.attack;
        if (delta != 0)
        {
            attacker.ModifyAttack(delta);
        }
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
