public class BaseCardContext
{
    public string ActingPlayerKey;
    public string OpponentPlayerKey;
    public IBoardStateReader Board;

    public string ActingPlayerId
    {
        get => ActingPlayerKey;
        set => ActingPlayerKey = value;
    }

    public string OpponentPlayerId
    {
        get => OpponentPlayerKey;
        set => OpponentPlayerKey = value;
    }
}

public sealed class CardValidationContext : BaseCardContext
{
}

public sealed class CardEffectContext : BaseCardContext
{
    public ICardStateWriter Writer;
}
