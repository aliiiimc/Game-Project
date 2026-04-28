using System;
using UnityEngine;

[Serializable]
public class CardRuntimeState
{
    [SerializeField] private CardData sourceCard;

    [SerializeField] private CardZone currentZone;

    [SerializeField] private AxialCoord boardPosition;

    [SerializeField] private OptionalInt currentMovementCapacity;

    [SerializeField] private OptionalInt currentHp;
    [SerializeField] private OptionalInt currentDamage;
    [SerializeField] private OptionalInt currentRevenue;

    [SerializeField] private SpellEffectType spellEffectType;
    [SerializeField] private OptionalInt spellEffectPower;
    [SerializeField] private int remainingEffectDurationTurns;

    [SerializeField] private bool isReadyToAttack;

    public CardData SourceCard => sourceCard;
    public CardZone CurrentZone => currentZone;
    public bool IsManifestedOnBoard => currentZone == CardZone.Board;
    public AxialCoord BoardPosition => boardPosition;
    public OptionalInt CurrentMovementCapacity => currentMovementCapacity;
    public OptionalInt CurrentHp => currentHp;
    public OptionalInt CurrentDamage => currentDamage;
    public OptionalInt CurrentRevenue => currentRevenue;
    public bool IsReadyToAttack => isReadyToAttack;
    public SpellEffectType SpellEffectType => spellEffectType;
    public OptionalInt SpellEffectPower => spellEffectPower;
    public int RemainingEffectDurationTurns => remainingEffectDurationTurns;
    public bool HasPersistentEffect => remainingEffectDurationTurns > 0;

    public CardRuntimeState(CardData sourceCard)
    {
        if (sourceCard == null)
        {
            throw new ArgumentNullException(nameof(sourceCard));
        }

        this.sourceCard = sourceCard;
        currentMovementCapacity = sourceCard.MovementCapacity.HasValue
            ? new OptionalInt(sourceCard.MovementCapacity.Value)
            : OptionalInt.None;
        currentHp = OptionalInt.None;
        currentDamage = OptionalInt.None;
        currentRevenue = OptionalInt.None;
        spellEffectType = default;
        spellEffectPower = OptionalInt.None;
        remainingEffectDurationTurns = 0;

        InitializeCardSpecificState();
    }

    private void InitializeCardSpecificState()
    {
        if (sourceCard is CharacterCardData characterCard)
        {
            currentHp = new OptionalInt(characterCard.maxHp);
            currentDamage = new OptionalInt(characterCard.attackDamage);
            isReadyToAttack = characterCard.startsReadyToAttack;
            return;
        }

        if (sourceCard is WorldEffectCardData worldEffectCard)
        {
            currentHp = worldEffectCard.structureHp;
            currentDamage = worldEffectCard.structureDamage;
            currentRevenue = worldEffectCard.revenuePerTurn;
            return;
        }

        if (sourceCard is SpellCardData spellCard)
        {
            spellEffectType = spellCard.effectType;
            spellEffectPower = new OptionalInt(spellCard.effectPower);
            remainingEffectDurationTurns = Mathf.Max(0, spellCard.effectDurationTurns);
            currentDamage = new OptionalInt(spellCard.effectPower);
            return;
        }
    }

    public void MoveToZone(CardZone zone)
    {
        currentZone = zone;

        if (zone != CardZone.Board)
        {
            boardPosition = default;
        }
    }

    public void ManifestOnBoard(AxialCoord position)
    {
        currentZone = CardZone.Board;
        boardPosition = position;
    }

    public void ConsumeMovement(int movementCost)
    {
        if (!currentMovementCapacity.HasValue)
        {
            return;
        }

        currentMovementCapacity = new OptionalInt(
            Mathf.Max(0, currentMovementCapacity.Value - Mathf.Max(0, movementCost)));
    }

    public void ResetMovementFromDefinition()
    {
        if (sourceCard == null)
        {
            currentMovementCapacity = OptionalInt.None;
            return;
        }

        if (sourceCard.MovementCapacity.HasValue)
        {
            currentMovementCapacity = new OptionalInt(sourceCard.MovementCapacity.Value);
            return;
        }

        currentMovementCapacity = OptionalInt.None;
    }

    public void SetAttackReady(bool ready)
    {
        isReadyToAttack = ready;
    }

    public void ConsumeEffectDurationTurn()
    {
        if (remainingEffectDurationTurns <= 0)
        {
            return;
        }

        remainingEffectDurationTurns--;
    }

    public void ResetEffectDurationFromDefinition()
    {
        if (sourceCard is SpellCardData spellCard)
        {
            remainingEffectDurationTurns = Mathf.Max(0, spellCard.effectDurationTurns);
            return;
        }

        remainingEffectDurationTurns = 0;
    }

    public void ApplyDamage(int amount)
    {
        if (!currentHp.HasValue)
        {
            return;
        }

        int clampedAmount = Mathf.Max(0, amount);
        currentHp = new OptionalInt(Mathf.Max(0, currentHp.Value - clampedAmount));
    }

    public void ApplyHeal(int amount)
    {
        if (!currentHp.HasValue)
        {
            return;
        }

        int clampedAmount = Mathf.Max(0, amount);
        currentHp = new OptionalInt(currentHp.Value + clampedAmount);
    }

    public void ModifyDamage(int delta)
    {
        if (!currentDamage.HasValue)
        {
            return;
        }

        currentDamage = new OptionalInt(Mathf.Max(0, currentDamage.Value + delta));
    }

    public void ModifyMovement(int delta)
    {
        if (!currentMovementCapacity.HasValue)
        {
            return;
        }

        currentMovementCapacity = new OptionalInt(Mathf.Max(0, currentMovementCapacity.Value + delta));
    }

    public void AddRevenue(int amount)
    {
        if (!currentRevenue.HasValue)
        {
            currentRevenue = new OptionalInt(0);
        }

        int clampedAmount = Mathf.Max(0, amount);
        currentRevenue = new OptionalInt(currentRevenue.Value + clampedAmount);
    }
}
