public sealed class CardPlayRequest
{
    public string ActingPlayerId;
    public string OpponentPlayerId;

    public CardRuntimeState SourceCard;
    public CardTarget Target;

    public IBoardStateReader Board;
    public ICardStateWriter Writer;
    public ICardTargetValidator Validator;
    public ICardEffect Effect;
}
