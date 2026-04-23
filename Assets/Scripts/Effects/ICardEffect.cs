public interface ICardEffect
{
    string EffectId { get; }

    CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target);
}
