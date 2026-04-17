// Result structure returned by card effect implementations containing success/failure status, reason codes, and optional telemetry data.
public readonly struct CardEffectResult
{
    public bool Succeeded { get; }
    public string ReasonCode { get; }
    public string Message { get; }

    // Optional telemetry fields for UI feedback.
    public int DamageDealt { get; }
    public int HealApplied { get; }
    public int RevenueGained { get; }

    private CardEffectResult(bool succeeded, string reasonCode, string message, int damageDealt, int healApplied, int revenueGained)
    {
        Succeeded = succeeded;
        ReasonCode = reasonCode;
        Message = message;
        DamageDealt = damageDealt;
        HealApplied = healApplied;
        RevenueGained = revenueGained;
    }

    public static CardEffectResult Success(string message = "", int damageDealt = 0, int healApplied = 0, int revenueGained = 0)
    {
        return new CardEffectResult(true, string.Empty, message ?? string.Empty, damageDealt, healApplied, revenueGained);
    }

    public static CardEffectResult Failure(string reasonCode, string message)
    {
        return new CardEffectResult(false, reasonCode ?? "FAILED", message ?? "Effect failed.", 0, 0, 0);
    }
}
