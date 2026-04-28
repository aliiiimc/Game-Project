public static class CardEffectGuards
{
    public static bool TryRequireContextAndWriter(CardEffectContext context, out CardEffectResult failure)
    {
        if (context == null)
        {
            failure = CardEffectResult.Failure("NO_CONTEXT", "Effect context is missing.");
            return false;
        }

        if (context.Writer == null)
        {
            failure = CardEffectResult.Failure("NO_WRITER", "State writer is missing.");
            return false;
        }

        failure = default;
        return true;
    }

    public static bool TryRequireSourceCard(CardRuntimeState sourceCard, out CardEffectResult failure)
    {
        if (sourceCard == null || sourceCard.SourceCard == null)
        {
            failure = CardEffectResult.Failure("NO_CARD", "Source card is missing.");
            return false;
        }

        failure = default;
        return true;
    }

    public static bool TryRequireTargetCard(CardTarget target, string effectLabel, out CardEffectResult failure)
    {
        if (target.targetCard == null)
        {
            failure = CardEffectResult.Failure("NO_TARGET_CARD", $"{effectLabel} effect needs a target card.");
            return false;
        }

        failure = default;
        return true;
    }

    public static bool TryRequireTargetType(CardTarget target, CardTargetType requiredType, string message, out CardEffectResult failure)
    {
        if (target.type != requiredType)
        {
            string fallbackMessage = $"Target type must be '{requiredType}'.";
            failure = CardEffectResult.Failure("WRONG_TARGET", string.IsNullOrWhiteSpace(message) ? fallbackMessage : message);
            return false;
        }

        failure = default;
        return true;
    }

    public static bool TryRequireBoardAndValidTile(CardEffectContext context, AxialCoord tile, string invalidTileMessage, out CardEffectResult failure)
    {
        if (context?.Board == null)
        {
            failure = CardEffectResult.Failure("NO_BOARD", "Board state reader is missing.");
            return false;
        }

        if (!context.Board.IsTileValid(tile))
        {
            failure = CardEffectResult.Failure("INVALID_TILE", string.IsNullOrWhiteSpace(invalidTileMessage) ? "Target tile is invalid." : invalidTileMessage);
            return false;
        }

        failure = default;
        return true;
    }

    public static bool TryRequireTileEmpty(CardEffectContext context, AxialCoord tile, string occupiedMessage, out CardEffectResult failure)
    {
        if (context?.Board == null)
        {
            failure = CardEffectResult.Failure("NO_BOARD", "Board state reader is missing.");
            return false;
        }

        if (context.Board.IsTileOccupied(tile))
        {
            failure = CardEffectResult.Failure("OCCUPIED_TILE", string.IsNullOrWhiteSpace(occupiedMessage) ? "Target tile is occupied." : occupiedMessage);
            return false;
        }

        failure = default;
        return true;
    }
}
