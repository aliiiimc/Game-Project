public struct CardEffectResult
{
    public bool Succeeded { get; private set; }
    public string ReasonCode { get; private set; }
    public string Message { get; private set; }

    public int DamageDealt { get; private set; }
    public int HealApplied { get; private set; }
    public int RevenueGained { get; private set; }

    public static CardEffectResult Success(string message = "", int damageDealt = 0, int healApplied = 0, int revenueGained = 0)
    {
        CardEffectResult result = new CardEffectResult();
        result.Succeeded = true;
        result.ReasonCode = string.Empty;
        result.Message = message ?? string.Empty;
        result.DamageDealt = damageDealt;
        result.HealApplied = healApplied;
        result.RevenueGained = revenueGained;
        return result;
    }

    public static CardEffectResult Failure(string reasonCode, string message)
    {
        CardEffectResult result = new CardEffectResult();
        result.Succeeded = false;
        result.ReasonCode = reasonCode ?? "FAILED";
        result.Message = message ?? "Effect failed.";
        result.DamageDealt = 0;
        result.HealApplied = 0;
        result.RevenueGained = 0;
        return result;
    }
}
