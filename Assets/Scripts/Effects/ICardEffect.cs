// Interface implemented by all concrete card effects (damage, heal, summon, etc.).
public interface ICardEffect
{
    // Stable id used for lookups from data assets.
    string EffectId { get; }

    // Executes card effect logic.
    CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target);
}
