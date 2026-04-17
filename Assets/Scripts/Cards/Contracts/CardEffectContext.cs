// Context object passed to card effects containing player identities, board state reader, and card state writer interfaces.
// Decouples effect implementations from concrete game manager dependencies.
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
