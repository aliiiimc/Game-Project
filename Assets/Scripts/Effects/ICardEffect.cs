// Interface for concrete card effect implementations (damage, healing, summoning, etc.) that execute card ability logic.
public interface ICardEffect
{
    // Stable id used for lookups from data assets.
    string EffectId { get; }

    // Executes card effect logic.
    CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target);
}
