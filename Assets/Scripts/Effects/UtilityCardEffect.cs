using UnityEngine;

public sealed class UtilityCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.utility";
    [SerializeField] private int movementDelta = 1;

    public string EffectId => effectId;

    public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Utility", out failure))
        {
            return failure;
        }

        int safeDelta = Mathf.Max(0, movementDelta);
        if (safeDelta <= 0)
        {
            return CardEffectResult.Failure("NO_UTILITY_VALUE", "Set movement delta above zero.");
        }

        context.Writer.ModifyMovement(target.targetCard, safeDelta);
        return CardEffectResult.Success("Utility effect applied.");
    }
}
