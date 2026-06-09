using UnityEngine;

public class Unit : MonoBehaviour
{
    private static readonly Color PlayerFullHealthColor = Color.white;
    private static readonly Color EnemyFullHealthColor = new Color(1f, 0.55f, 0.58f);
    private static readonly Color DamagedTintColor = new Color(1f, 0.35f, 0.35f);

    public int moveRange = 2;
    public int attackRange = 1;
    public int health = 10;
    public int attack = 3;
    public string owner = "player";
    public HexTile currentTile;
    public HexTile turnStartTile;
    public bool hasMovedThisTurn;
    public bool hasAttackedThisTurn;
    public int movementSpentThisTurn;
    public CharacterCardData sourceCharacterCardData;
    public int movementRangeBonus;
    public int movementRangeBonusTurnsRemaining;
    public int frozenTurnsRemaining;

    private CardRuntimeState runtimeCard;

    // Ali: runtime flag copied from card data for world-effect capture rules.
    public bool canColonizeEnemyWorldEffects;


    // Ali: track whether a spawned unit is allowed to attack yet, instead of assuming every new unit is immediately ready.
    public bool isReadyToAttack = true;

    public CardRuntimeState RuntimeCard => runtimeCard;
    public bool IsFrozen => frozenTurnsRemaining > 0;


    public bool CanMove()
    {
        return !IsFrozen && !hasAttackedThisTurn && GetRemainingMovement() > 0;
    }


    public bool CanAttack()
    {
        return !IsFrozen && isReadyToAttack && !hasAttackedThisTurn && !HasExhaustedMovementThisTurn();
    }

    public int GetRemainingMovement()
    {
        if (IsFrozen)
        {
            return 0;
        }

        return Mathf.Max(0, GetEffectiveMoveRange() - movementSpentThisTurn);
    }

    public bool HasExhaustedMovementThisTurn()
    {
        return hasMovedThisTurn && GetRemainingMovement() <= 0;
    }

    public void MarkMoved(int movementCost)
    {
        int safeCost = Mathf.Max(0, movementCost);
        movementSpentThisTurn = Mathf.Min(GetEffectiveMoveRange(), movementSpentThisTurn + safeCost);
        hasMovedThisTurn = movementSpentThisTurn > 0;
    }

    public void MarkAttacked()
    {
        hasAttackedThisTurn = true;
    }

    public void LinkRuntimeCard(CardRuntimeState card)
    {
        runtimeCard = card;
        SyncStatsFromRuntimeCard();
    }

    public void ApplyDamage(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);

        if (runtimeCard != null)
        {
            runtimeCard.ApplyDamage(safeAmount);
            SyncStatsFromRuntimeCard();
        }
        else
        {
            health = Mathf.Max(0, health - safeAmount);
        }
        
        string unitName = sourceCharacterCardData != null ? sourceCharacterCardData.DisplayName : "Unit";
        var hud = FindFirstObjectByType<FortGame.UI.HUDManager>();

        if (health <= 0)
        {
            hud?.ShowSpellAnnouncement($"{unitName} took {safeAmount} damage and was destroyed! [HP: 0]");
            Die();
            return;
        }

        hud?.ShowSpellAnnouncement($"{unitName} took {safeAmount} damage. [HP: {health}]");
        RefreshVisualState();
    }

    public void ApplyHeal(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);

        if (runtimeCard != null)
        {
            runtimeCard.ApplyHeal(safeAmount);
            SyncStatsFromRuntimeCard();
            
            string name = sourceCharacterCardData != null ? sourceCharacterCardData.DisplayName : "Unit";
            FindFirstObjectByType<FortGame.UI.HUDManager>()?.ShowSpellAnnouncement($"{name} was healed for {safeAmount}. [HP: {health}]");
            
            RefreshVisualState();
            return;
        }

        int maxHp = sourceCharacterCardData != null ? Mathf.Max(0, sourceCharacterCardData.maxHp) : 0;
        health = maxHp > 0
            ? Mathf.Min(maxHp, health + safeAmount)
            : health + safeAmount;
            
        string unitName = sourceCharacterCardData != null ? sourceCharacterCardData.DisplayName : "Unit";
        FindFirstObjectByType<FortGame.UI.HUDManager>()?.ShowSpellAnnouncement($"{unitName} was healed for {safeAmount}. [HP: {health}]");
            
        RefreshVisualState();
    }

    public void ModifyAttack(int delta)
    {
        if (runtimeCard != null)
        {
            runtimeCard.ModifyDamage(delta);
            SyncStatsFromRuntimeCard();
            return;
        }

        attack = Mathf.Max(0, attack + delta);
    }

    public void ModifyMovementRange(int delta)
    {
        if (runtimeCard != null)
        {
            runtimeCard.ModifyMovement(delta);
            SyncStatsFromRuntimeCard();
            return;
        }

        moveRange = Mathf.Max(0, moveRange + delta);
        movementSpentThisTurn = Mathf.Min(movementSpentThisTurn, GetEffectiveMoveRange());
    }

    private void SyncStatsFromRuntimeCard()
    {
        if (runtimeCard == null)
        {
            return;
        }

        if (runtimeCard.CurrentHp.HasValue)
        {
            health = runtimeCard.CurrentHp.Value;
        }

        if (runtimeCard.CurrentDamage.HasValue)
        {
            attack = runtimeCard.CurrentDamage.Value;
        }

        if (runtimeCard.CurrentMovementCapacity.HasValue)
        {
            moveRange = runtimeCard.CurrentMovementCapacity.Value;
            movementSpentThisTurn = Mathf.Min(movementSpentThisTurn, GetEffectiveMoveRange());
        }

        RefreshVisualState();
    }

    public int GetEffectiveMoveRange()
    {
        int bonus = movementRangeBonusTurnsRemaining > 0
            ? movementRangeBonus
            : 0;

        return Mathf.Max(0, moveRange + bonus);
    }

    public void ApplyMovementRangeBonus(int bonus, int turns)
    {
        int safeTurns = Mathf.Max(1, turns);

        movementRangeBonus = bonus;
        movementRangeBonusTurnsRemaining = safeTurns;
        movementSpentThisTurn = Mathf.Min(movementSpentThisTurn, GetEffectiveMoveRange());
    }

    public void ApplyFreeze(int turns)
    {
        int safeTurns = Mathf.Max(1, turns);
        frozenTurnsRemaining = Mathf.Max(frozenTurnsRemaining, safeTurns);
    }

    public void ConsumeTimedEffectsOnOwnerTurnEnd()
    {
        if (frozenTurnsRemaining > 0)
        {
            frozenTurnsRemaining = Mathf.Max(0, frozenTurnsRemaining - 1);
        }

        if (movementRangeBonusTurnsRemaining <= 0)
        {
            return;
        }

        movementRangeBonusTurnsRemaining = Mathf.Max(0, movementRangeBonusTurnsRemaining - 1);
        if (movementRangeBonusTurnsRemaining <= 0)
        {
            movementRangeBonus = 0;
        }

        movementSpentThisTurn = Mathf.Min(movementSpentThisTurn, GetEffectiveMoveRange());
    }

    public void ResetTurnActions()
    {
        hasMovedThisTurn = false;
        hasAttackedThisTurn = false;
        movementSpentThisTurn = 0;
        turnStartTile = currentTile;
        // A unit that was not ready on summon becomes ready on its owner's next turn.
        isReadyToAttack = true;
    }

    public void PlaceOnTile(HexTile tile, bool snapToTile = true)
    {
        currentTile = tile;
        if (turnStartTile == null)
        {
            turnStartTile = tile;
        }
        tile.PlaceUnit(owner);

        if (snapToTile)
        {
            transform.position = tile.transform.position;
        }

        RefreshVisualState();
    }

    public void Die()
    {
        DeathHistoryManager.GetOrCreate().RecordCharacterDeath(runtimeCard, owner);
        runtimeCard?.MoveToZone(CardZone.Discard);

        if (currentTile != null)
        {
            currentTile.ClearUnitOccupant();
            currentTile = null;
        }
        Destroy(gameObject);
    }

    private void RefreshVisualState()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            return;
        }

        Color ownerColor = owner == "player" ? PlayerFullHealthColor : EnemyFullHealthColor;
        int maxHp = sourceCharacterCardData != null ? Mathf.Max(0, sourceCharacterCardData.maxHp) : 0;
        if (maxHp <= 0)
        {
            sr.color = ownerColor;
            return;
        }

        float hpRatio = Mathf.Clamp01(health / (float)maxHp);
        sr.color = Color.Lerp(DamagedTintColor, ownerColor, hpRatio);
    }
}
