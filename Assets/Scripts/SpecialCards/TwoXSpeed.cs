using UnityEngine;

public sealed class TwoXSpeed
{
    private const string CardName = "+2 speed";

    public bool IsMatch(CardRuntimeState sourceCard)
    {
        if (sourceCard == null || !(sourceCard.SourceCard is SpeedSpellCardData))
        {
            return false;
        }

        return sourceCard.SourceCard.MatchesSpecialCard(SpecialCardIds.SpellTwoXSpeed, CardName);
    }

    public CardEffectResult Apply(CardRuntimeState sourceCard, CardTarget target)
    {
        if (!(sourceCard?.SourceCard is SpeedSpellCardData speedSpellCard))
        {
            return CardEffectResult.Failure("NO_SPEED_SPELL", "TwoXSpeed needs a speed spell card source.");
        }

        if (target.targetCard == null)
        {
            return CardEffectResult.Failure("NO_TARGET_CARD", "TwoXSpeed needs a target unit card.");
        }

        int movementCapacityBonus = speedSpellCard.movementCapacityBonus;
        if (movementCapacityBonus <= 0)
        {
            return CardEffectResult.Failure("INVALID_BONUS", "TwoXSpeed needs a movement bonus above zero.");
        }

        int durationTurns = Mathf.Max(0, speedSpellCard.effectDurationTurns);
        if (durationTurns <= 0)
        {
            return CardEffectResult.Failure("INVALID_DURATION", "TwoXSpeed needs effectDurationTurns above zero.");
        }

        Unit targetUnit = FindUnitForCard(target.targetCard);
        if (targetUnit == null)
        {
            return CardEffectResult.Failure("NO_TARGET_UNIT", "TwoXSpeed could not resolve the targeted board unit.");
        }

        targetUnit.ApplyMovementRangeBonus(movementCapacityBonus, durationTurns);
        return CardEffectResult.Success($"Movement capacity +{movementCapacityBonus} applied for {durationTurns} turn(s).");
    }

    private static Unit FindUnitForCard(CardRuntimeState card)
    {
        if (card == null)
        {
            return null;
        }

        Unit[] units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Unit unit = units[i];
            if (unit != null && ReferenceEquals(unit.RuntimeCard, card))
            {
                return unit;
            }
        }

        return null;
    }
}
