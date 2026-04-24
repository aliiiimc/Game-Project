using UnityEngine;

public sealed class ReusableTargetRulesValidator : MonoBehaviour, ICardTargetValidator
{
    [SerializeField] private string validatorId = "target.rules.reusable";
    [SerializeField] private bool requireFreeTile = true;

    public string ValidatorId => validatorId;

    public CardValidationResult Validate(CardValidationContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (context == null)
        {
            return CardValidationResult.Invalid("NO_CONTEXT", "Validation context is missing.");
        }

        if (sourceCard == null || sourceCard.SourceCard == null)
        {
            return CardValidationResult.Invalid("NO_CARD", "Source card is missing.");
        }

        if (target.type == CardTargetType.AllyUnit)
        {
            return ValidateUnitTarget(context, target, shouldBeAlly: true);
        }

        if (target.type == CardTargetType.EnemyUnit)
        {
            return ValidateUnitTarget(context, target, shouldBeAlly: false);
        }

        if (target.type == CardTargetType.Tile)
        {
            return ValidateTileTarget(context, target);
        }

        return CardValidationResult.Invalid("UNSUPPORTED_TARGET", $"Target type '{target.type}' is not supported.");
    }

    private CardValidationResult ValidateTileTarget(CardValidationContext context, CardTarget target)
    {
        if (context.Board == null)
        {
            return CardValidationResult.Invalid("NO_BOARD", "Board state reader is missing.");
        }

        if (!context.Board.IsTileValid(target.tile))
        {
            return CardValidationResult.Invalid("INVALID_TILE", "Tile is outside board bounds.");
        }

        if (requireFreeTile && context.Board.IsTileOccupied(target.tile))
        {
            return CardValidationResult.Invalid("TILE_OCCUPIED", "Tile is occupied.");
        }

        return CardValidationResult.Valid();
    }

    private static CardValidationResult ValidateUnitTarget(CardValidationContext context, CardTarget target, bool shouldBeAlly)
    {
        if (target.targetCard == null)
        {
            return CardValidationResult.Invalid("NO_TARGET_CARD", "Unit target requires target card.");
        }

        if (!(target.targetCard.SourceCard is CharacterCardData))
        {
            return CardValidationResult.Invalid("NOT_UNIT", "Target card is not a unit.");
        }

        if (!target.targetCard.IsManifestedOnBoard)
        {
            return CardValidationResult.Invalid("NOT_ON_BOARD", "Target unit is not on the board.");
        }

        if (string.IsNullOrWhiteSpace(target.targetPlayerId))
        {
            return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Unit target player id is required.");
        }

        string expected = shouldBeAlly ? context.ActingPlayerKey : context.OpponentPlayerKey;
        if (target.targetPlayerId != expected)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", shouldBeAlly
                ? "Target unit is not allied."
                : "Target unit is not an enemy.");
        }

        return CardValidationResult.Valid();
    }

}