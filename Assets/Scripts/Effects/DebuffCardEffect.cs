using UnityEngine;

public sealed class DebuffCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.debuff";
    [SerializeField] private int damageAmount = 0;
    [SerializeField] private int damageReductionAmount = 1;
    [SerializeField] private int speedReductionAmount = 1;

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
            return CardEffectResult.Failure("NO_TARGET_CARD", "Debuff effect needs a target card.");
        }

        int safeDamage = Mathf.Max(0, damageAmount);
        int safeDamageReduction = Mathf.Max(0, damageReductionAmount);
        int safeSpeedReduction = Mathf.Max(0, speedReductionAmount);
        bool didSomething = false;

        if (safeDamage > 0)
        {
            context.Writer.ApplyDamage(target.targetCard, safeDamage);
            didSomething = true;
        }

        if (safeDamageReduction > 0)
        {
            context.Writer.ModifyDamage(target.targetCard, -safeDamageReduction);
            didSomething = true;
        }

        if (safeSpeedReduction > 0)
        {
            context.Writer.ModifyMovement(target.targetCard, -safeSpeedReduction);
            didSomething = true;
        }

        if (!didSomething)
        {
            return CardEffectResult.Failure("NO_DEBUFF_VALUES", "Set at least one debuff value above zero.");
        }

        return CardEffectResult.Success("Debuff applied.", damageDealt: safeDamage);
    }
}
