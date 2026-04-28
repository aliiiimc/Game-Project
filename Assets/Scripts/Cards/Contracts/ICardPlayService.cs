public interface ICardPlayService
{
    CardPlayResult CanPlayCard(CardRuntimeState sourceCard, string actingPlayerId, CardTarget target);

    CardPlayResult PlayCard(CardRuntimeState sourceCard, string actingPlayerId, CardTarget target);
}
