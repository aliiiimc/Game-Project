public sealed class CardValidationContext
{
    public string ActingPlayerId;
    public string OpponentPlayerId;
    public IBoardStateReader Board;

    public string ActingPlayerKey
    {
        get => ActingPlayerId;
        set => ActingPlayerId = value;
    }

    public string OpponentPlayerKey
    {
        get => OpponentPlayerId;
        set => OpponentPlayerId = value;
    }
}
