// Result returned by a target validator before card execution.
public readonly struct CardValidationResult
{
    public bool IsValid { get; }
    public string ReasonCode { get; }
    public string Message { get; }

    private CardValidationResult(bool isValid, string reasonCode, string message)
    {
        IsValid = isValid;
        ReasonCode = reasonCode;
        Message = message;
    }

    public static CardValidationResult Valid()
    {
        return new CardValidationResult(true, string.Empty, string.Empty);
    }

    public static CardValidationResult Invalid(string reasonCode, string message)
    {
        return new CardValidationResult(false, reasonCode ?? "UNKNOWN", message ?? "Invalid target.");
    }
}
