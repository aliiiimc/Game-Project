using UnityEngine;

public sealed class DamageCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.damage";
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

        if (target.targetCard == null)
        {
            return CardEffectResult.Failure("NO_TARGET_CARD", "Damage effect needs a target card.");
        }

        int safeAmount = Mathf.Max(0, amount);
        context.Writer.ApplyDamage(target.targetCard, safeAmount);

        return CardEffectResult.Success("Damage applied.", damageDealt: safeAmount);
    }
}
