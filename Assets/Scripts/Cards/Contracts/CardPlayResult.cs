public struct CardPlayResult
{
    public bool Succeeded { get; private set; }
    public string ReasonCode { get; private set; }
    public string Message { get; private set; }

    public bool CostWasSpent { get; private set; }
    public CardZone FinalZone { get; private set; }

    public CardValidationResult ValidationResult { get; private set; }
    public CardEffectResult EffectResult { get; private set; }

    public static CardPlayResult Success(
        CardValidationResult validationResult,
        CardEffectResult effectResult,
        bool costWasSpent,
        CardZone finalZone,
        string message)
    {
        CardPlayResult result = new CardPlayResult();
        result.Succeeded = true;
        result.ReasonCode = string.Empty;
        result.Message = message ?? string.Empty;
        result.CostWasSpent = costWasSpent;
        result.FinalZone = finalZone;
        result.ValidationResult = validationResult;
        result.EffectResult = effectResult;
        return result;
    }

    public static CardPlayResult Failure(
        string reasonCode,
        string message,
        CardValidationResult validationResult,
        CardEffectResult effectResult,
        bool costWasSpent,
        CardZone finalZone)
    {
        CardPlayResult result = new CardPlayResult();
        result.Succeeded = false;
        result.ReasonCode = reasonCode ?? "PLAY_FAILED";
        result.Message = message ?? "Card play failed.";
        result.CostWasSpent = costWasSpent;
        result.FinalZone = finalZone;
        result.ValidationResult = validationResult;
        result.EffectResult = effectResult;
        return result;
    }
}
