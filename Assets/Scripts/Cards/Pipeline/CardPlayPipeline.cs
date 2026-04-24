public sealed class CardPlayPipeline : ICardPlayPipeline
{
    public CardPlayResult Play(CardPlayRequest request)
    {
        CardValidationResult emptyValidation = CardValidationResult.Valid();
        CardEffectResult emptyEffect = CardEffectResult.Success();

        if (request == null)
        {
            return CardPlayResult.Failure(
                "NO_REQUEST",
                "Card play request is missing.",
                emptyValidation,
                emptyEffect,
                costWasSpent: false,
                finalZone: CardZone.Hand);
        }

        if (request.SourceCard == null || request.SourceCard.SourceCard == null)
        {
            return CardPlayResult.Failure(
                "NO_CARD",
                "Source card is missing.",
                emptyValidation,
                emptyEffect,
                costWasSpent: false,
                finalZone: CardZone.Hand);
        }

        if (request.Validator == null)
        {
            return CardPlayResult.Failure(
                "NO_VALIDATOR",
                "Target validator is missing.",
                emptyValidation,
                emptyEffect,
                costWasSpent: false,
                finalZone: request.SourceCard.CurrentZone);
        }

        if (request.Effect == null)
        {
            return CardPlayResult.Failure(
                "NO_EFFECT",
                "Card effect is missing.",
                emptyValidation,
                emptyEffect,
                costWasSpent: false,
                finalZone: request.SourceCard.CurrentZone);
        }

        if (request.Writer == null)
        {
            return CardPlayResult.Failure(
                "NO_WRITER",
                "State writer is missing.",
                emptyValidation,
                emptyEffect,
                costWasSpent: false,
                finalZone: request.SourceCard.CurrentZone);
        }

        CardValidationContext validationContext = new CardValidationContext
        {
            ActingPlayerKey = request.ActingPlayerId,
            OpponentPlayerKey = request.OpponentPlayerId,
            Board = request.Board
        };

        CardValidationResult validationResult = request.Validator.Validate(validationContext, request.SourceCard, request.Target);
        if (!validationResult.IsValid)
        {
            return CardPlayResult.Failure(
                validationResult.ReasonCode,
                validationResult.Message,
                validationResult,
                emptyEffect,
                costWasSpent: false,
                finalZone: request.SourceCard.CurrentZone);
        }

        int safeCost = request.SourceCard.SourceCard.cost < 0 ? 0 : request.SourceCard.SourceCard.cost;
        bool spendSucceeded = request.Writer.TrySpendCost(request.ActingPlayerId, safeCost);
        if (!spendSucceeded)
        {
            return CardPlayResult.Failure(
                "INSUFFICIENT_FUNDS",
                "Player does not have enough money to play this card.",
                validationResult,
                emptyEffect,
                costWasSpent: false,
                finalZone: request.SourceCard.CurrentZone);
        }

        CardEffectContext effectContext = new CardEffectContext
        {
            ActingPlayerKey = request.ActingPlayerId,
            OpponentPlayerKey = request.OpponentPlayerId,
            Board = request.Board,
            Writer = request.Writer
        };

        CardEffectResult effectResult = request.Effect.Apply(effectContext, request.SourceCard, request.Target);
        if (!effectResult.Succeeded)
        {
            return CardPlayResult.Failure(
                "EFFECT_FAILED_AFTER_SPEND",
                effectResult.Message,
                validationResult,
                effectResult,
                costWasSpent: true,
                finalZone: request.SourceCard.CurrentZone);
        }

        CardZone finalZone = request.SourceCard.IsManifestedOnBoard ? CardZone.Board : CardZone.Discard;
        if (finalZone == CardZone.Discard)
        {
            request.Writer.MoveCardToZone(request.SourceCard, CardZone.Discard);
        }

        return CardPlayResult.Success(
            validationResult,
            effectResult,
            costWasSpent: true,
            finalZone: finalZone,
            message: "Card play completed.");
    }
}
