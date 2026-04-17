// Context object passed to target validators containing player identities and read-only board state for validation checks.
public sealed class CardValidationContext
{
    public string ActingPlayerId;
    public string OpponentPlayerId;
    public IBoardStateReader Board;

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
