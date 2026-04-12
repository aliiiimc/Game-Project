// Context passed to card effects. Keeps effects decoupled from concrete managers.
public sealed class CardEffectContext
{
    public string ActingPlayerId;
    public string OpponentPlayerId;

    public IBoardStateReader Board;
    public ICardStateWriter Writer;

    // Alias properties keep contract names consistent with board owner keys.
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
