using UnityEngine;

public sealed class DebugTargetValidator : MonoBehaviour, ICardTargetValidator
{
    [SerializeField] private string validatorId = "debug.target.basic";
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

        if (context.Board == null)
        {
            return CardValidationResult.Invalid("NO_BOARD", "Board state reader is missing.");
        }

        if (target.type == CardTargetType.Tile)
        {
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

        if (target.type == CardTargetType.EnemyFort)
        {
            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Enemy fort target player is required.");
            }

            if (target.targetPlayerId != context.OpponentPlayerKey)
            {
                return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", "Target player is not the current opponent.");
            }

            return CardValidationResult.Valid();
        }

        if (target.type == CardTargetType.Player)
        {
            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Player target id is missing.");
            }

            return CardValidationResult.Valid();
        }

        return CardValidationResult.Invalid("UNSUPPORTED_TARGET", $"Target type '{target.type}' is not supported by DebugTargetValidator.");
    }
}
