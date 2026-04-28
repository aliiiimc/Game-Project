using UnityEngine;

public static class FallbackEffectFactory
{
    public static ICardEffect Create(CardData cardData)
    {
        if (cardData is CharacterCardData || cardData is WorldEffectCardData)
        {
            return new ManifestEffect();
        }

        if (cardData is SpellCardData spellCard)
        {
            switch (spellCard.effectType)
            {
                case SpellEffectType.Damage:
                    return new SpellDamageEffect(spellCard.effectPower);
                case SpellEffectType.Heal:
                    return new SpellHealEffect(spellCard.effectPower);
                case SpellEffectType.Buff:
                    return new SpellBuffEffect(spellCard.effectPower);
                case SpellEffectType.Debuff:
                    return new SpellDebuffEffect(spellCard.effectPower);
                case SpellEffectType.Boost:
                    return new IncomeEffect(spellCard.effectPower);
                case SpellEffectType.Summon:
                    return new ManifestEffect();
                case SpellEffectType.Utility:
                    return new UtilityTempoEffect(spellCard.effectPower);
            }
        }

        return new UnsupportedEffect();
    }

    private sealed class ManifestEffect : ICardEffect
    {
        public string EffectId => "effect.fallback.manifest";

        public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
            {
                return failure;
            }

            if (!CardEffectGuards.TryRequireSourceCard(sourceCard, out failure))
            {
                return failure;
            }

            if (!CardEffectGuards.TryRequireTargetType(target, CardTargetType.Tile, "Card requires a tile target.", out failure))
            {
                return failure;
            }

            context.Writer.ManifestCard(sourceCard, target.tile);
            return CardEffectResult.Success("Card manifested.");
        }
    }

    private abstract class TargetCardEffectBase : ICardEffect
    {
        protected readonly int amount;

        protected TargetCardEffectBase(int amount)
        {
            this.amount = Mathf.Max(0, amount);
        }

        public abstract string EffectId { get; }
        protected abstract string MissingTargetLabel { get; }
        protected abstract CardEffectResult ApplyToTarget(ICardStateWriter writer, CardRuntimeState targetCard);

        public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
            {
                return failure;
            }

            if (!CardEffectGuards.TryRequireTargetCard(target, MissingTargetLabel, out failure))
            {
                return failure;
            }

            return ApplyToTarget(context.Writer, target.targetCard);
        }
    }

    private sealed class SpellDamageEffect : TargetCardEffectBase
    {
        public SpellDamageEffect(int amount)
            : base(amount)
        {
        }

        public override string EffectId => "effect.fallback.damage";
        protected override string MissingTargetLabel => "Damage spell";

        protected override CardEffectResult ApplyToTarget(ICardStateWriter writer, CardRuntimeState targetCard)
        {
            writer.ApplyDamage(targetCard, amount);
            return CardEffectResult.Success("Damage applied.", damageDealt: amount);
        }
    }

    private sealed class SpellHealEffect : TargetCardEffectBase
    {
        public SpellHealEffect(int amount)
            : base(amount)
        {
        }

        public override string EffectId => "effect.fallback.heal";
        protected override string MissingTargetLabel => "Heal spell";

        protected override CardEffectResult ApplyToTarget(ICardStateWriter writer, CardRuntimeState targetCard)
        {
            writer.ApplyHeal(targetCard, amount);
            return CardEffectResult.Success("Heal applied.", healApplied: amount);
        }
    }

    private sealed class SpellBuffEffect : TargetCardEffectBase
    {
        public SpellBuffEffect(int amount)
            : base(amount)
        {
        }

        public override string EffectId => "effect.fallback.buff";
        protected override string MissingTargetLabel => "Buff spell";

        protected override CardEffectResult ApplyToTarget(ICardStateWriter writer, CardRuntimeState targetCard)
        {
            writer.ModifyDamage(targetCard, amount);
            writer.ModifyMovement(targetCard, amount);
            return CardEffectResult.Success("Buff applied.");
        }
    }

    private sealed class SpellDebuffEffect : TargetCardEffectBase
    {
        public SpellDebuffEffect(int amount)
            : base(amount)
        {
        }

        public override string EffectId => "effect.fallback.debuff";
        protected override string MissingTargetLabel => "Debuff spell";

        protected override CardEffectResult ApplyToTarget(ICardStateWriter writer, CardRuntimeState targetCard)
        {
            writer.ModifyDamage(targetCard, -amount);
            writer.ModifyMovement(targetCard, -amount);
            return CardEffectResult.Success("Debuff applied.");
        }
    }

    private sealed class UtilityTempoEffect : TargetCardEffectBase
    {
        public UtilityTempoEffect(int amount)
            : base(amount)
        {
        }

        public override string EffectId => "effect.fallback.utility";
        protected override string MissingTargetLabel => "Utility spell";

        protected override CardEffectResult ApplyToTarget(ICardStateWriter writer, CardRuntimeState targetCard)
        {
            if (amount <= 0)
            {
                return CardEffectResult.Failure("NO_UTILITY_VALUE", "Utility effect power must be above zero.");
            }

            writer.ModifyMovement(targetCard, amount);
            return CardEffectResult.Success("Utility effect applied.");
        }
    }

    private sealed class IncomeEffect : ICardEffect
    {
        private readonly int amount;

        public IncomeEffect(int amount)
        {
            this.amount = Mathf.Max(0, amount);
        }

        public string EffectId => "effect.fallback.income";

        public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
            {
                return failure;
            }

            if (string.IsNullOrWhiteSpace(context.ActingPlayerKey))
            {
                return CardEffectResult.Failure("NO_ACTOR", "Acting player id is missing.");
            }

            context.Writer.AddRevenue(context.ActingPlayerKey, amount);
            return CardEffectResult.Success("Income added.", revenueGained: amount);
        }
    }

    private sealed class UnsupportedEffect : ICardEffect
    {
        public string EffectId => "effect.fallback.unsupported";

        public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            return CardEffectResult.Failure("NO_EFFECT_MAPPING", "No effect mapping found for this card.");
        }
    }
}
