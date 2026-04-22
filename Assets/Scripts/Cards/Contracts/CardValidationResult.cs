public struct CardValidationResult
{
    public bool IsValid { get; private set; }
    public string ReasonCode { get; private set; }
    public string Message { get; private set; }

    public static CardValidationResult Valid()
    {
        CardValidationResult result = new CardValidationResult();
        result.IsValid = true;
        result.ReasonCode = string.Empty;
        result.Message = string.Empty;
        return result;
    }

    public static CardValidationResult Invalid(string reasonCode, string message)
    {
        CardValidationResult result = new CardValidationResult();
        result.IsValid = false;
        result.ReasonCode = reasonCode ?? "UNKNOWN";
        result.Message = message ?? "Invalid target.";
        return result;
    }
}
