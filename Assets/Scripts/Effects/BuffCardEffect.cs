using UnityEngine;

public sealed class BuffCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.buff";
    [SerializeField] private int healAmount = 0;
    [SerializeField] private int damageBoostAmount = 1;
    [SerializeField] private int speedBoostAmount = 1;

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
            return CardEffectResult.Failure("NO_TARGET_CARD", "Buff effect needs a target card.");
        }

        int safeHeal = Mathf.Max(0, healAmount);
        int safeDamageBoost = Mathf.Max(0, damageBoostAmount);
        int safeSpeedBoost = Mathf.Max(0, speedBoostAmount);
        bool didSomething = false;

        if (safeHeal > 0)
        {
            context.Writer.ApplyHeal(target.targetCard, safeHeal);
            didSomething = true;
        }

        if (safeDamageBoost > 0)
        {
            context.Writer.ModifyDamage(target.targetCard, safeDamageBoost);
            didSomething = true;
        }

        if (safeSpeedBoost > 0)
        {
            context.Writer.ModifyMovement(target.targetCard, safeSpeedBoost);
            didSomething = true;
        }

        if (!didSomething)
        {
            return CardEffectResult.Failure("NO_BUFF_VALUES", "Set at least one buff value above zero.");
        }

        return CardEffectResult.Success("Buff applied.", healApplied: safeHeal);
    }
}
