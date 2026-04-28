using UnityEngine;

public sealed class PlayerKeyResolver
{
    public const string PlayerOneKey = "player";
    public const string PlayerTwoKey = "enemy";

    private readonly GameManager gameManager;

    public PlayerKeyResolver(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public bool TryResolveActorAndOpponent(string requestedPlayerId, out string actingPlayerKey, out string opponentPlayerKey)
    {
        actingPlayerKey = ResolveActingPlayerKey(requestedPlayerId);
        opponentPlayerKey = ResolveOpponentPlayerKey(actingPlayerKey);
        return !string.IsNullOrWhiteSpace(actingPlayerKey) && !string.IsNullOrWhiteSpace(opponentPlayerKey);
    }

    public PlayerState ResolveActingPlayerState(string actingPlayerKey)
    {
        if (gameManager == null)
        {
            return null;
        }

        if (IsMatch(gameManager.player1, actingPlayerKey, PlayerOneKey))
        {
            return gameManager.player1;
        }

        if (IsMatch(gameManager.player2, actingPlayerKey, PlayerTwoKey))
        {
            return gameManager.player2;
        }

        return null;
    }

    private string ResolveActingPlayerKey(string requestedPlayerId)
    {
        if (gameManager == null)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(requestedPlayerId))
        {
            return ResolveCurrentPlayerKey();
        }

        if (IsMatch(gameManager.player1, requestedPlayerId, PlayerOneKey))
        {
            return PlayerOneKey;
        }

        if (IsMatch(gameManager.player2, requestedPlayerId, PlayerTwoKey))
        {
            return PlayerTwoKey;
        }

        return requestedPlayerId;
    }

    private string ResolveCurrentPlayerKey()
    {
        if (gameManager?.currentPlayer == null)
        {
            return string.Empty;
        }

        if (ReferenceEquals(gameManager.currentPlayer, gameManager.player1))
        {
            return PlayerOneKey;
        }

        if (ReferenceEquals(gameManager.currentPlayer, gameManager.player2))
        {
            return PlayerTwoKey;
        }

        return gameManager.currentPlayer.playerName;
    }

    private string ResolveOpponentPlayerKey(string actingPlayerKey)
    {
        if (string.IsNullOrWhiteSpace(actingPlayerKey) || gameManager == null)
        {
            return string.Empty;
        }

        if (actingPlayerKey == PlayerOneKey || IsMatch(gameManager.player1, actingPlayerKey, PlayerOneKey))
        {
            return PlayerTwoKey;
        }

        if (actingPlayerKey == PlayerTwoKey || IsMatch(gameManager.player2, actingPlayerKey, PlayerTwoKey))
        {
            return PlayerOneKey;
        }

        return string.Empty;
    }

    private static bool IsMatch(PlayerState player, string playerId, string canonicalKey = null)
    {
        if (player == null || string.IsNullOrWhiteSpace(playerId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(canonicalKey) && playerId == canonicalKey)
        {
            return true;
        }

        return playerId == player.playerName;
    }
}
