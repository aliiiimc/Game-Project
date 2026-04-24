using UnityEngine;

public sealed class IncomeBoostCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.income_boost";
    [SerializeField] private int amount = 1;

    public string EffectId => effectId;

    public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (context == null)
        {
            return CardEffectResult.Failure("NO_CONTEXT", "Effect context is missing.");
        }

        if (context.Writer == null)
        {
            return CardEffectResult.Failure("NO_WRITER", "State writer is missing.");
        }

        if (string.IsNullOrWhiteSpace(context.ActingPlayerKey))
        {
            return CardEffectResult.Failure("NO_ACTOR", "Acting player id is missing.");
        }

        int safeAmount = Mathf.Max(0, amount);
        context.Writer.AddRevenue(context.ActingPlayerKey, safeAmount);

        return CardEffectResult.Success("Income boost applied.", revenueGained: safeAmount);
    }
}
