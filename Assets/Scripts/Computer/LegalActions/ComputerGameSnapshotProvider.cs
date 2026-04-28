using System.Collections.Generic;
using UnityEngine;

namespace FortGame.Computer
{
    /// <summary>
    /// Builds a lightweight game-state snapshot for legal action generation.
    /// </summary>
    public sealed class ComputerGameSnapshotProvider
    {
        public ComputerGameSnapshot CreateSnapshot(ComputerPlayer computer)
        {
            if (computer == null)
            {
                return null;
            }

            GameManager gameManager = computer.gameManager != null
                ? computer.gameManager
                : Object.FindFirstObjectByType<GameManager>();

            HexGrid hexGrid = computer.hexGrid != null
                ? computer.hexGrid
                : Object.FindFirstObjectByType<HexGrid>();

            if (gameManager == null || hexGrid == null || (computer.playerState == null && gameManager.player2 == null))
            {
                return null;
            }

            // Rabie: build AI decisions from the real enemy player state instead of only debug cards.
            PlayerState actingPlayer = ResolveActingPlayer(gameManager, computer);
            PlayerState opponentPlayer = ResolveOpponent(gameManager, actingPlayer);

            string actingPlayerKey = ResolvePlayerKey(gameManager, actingPlayer, "enemy");
            string opponentPlayerKey = ResolvePlayerKey(gameManager, opponentPlayer, "player");

            List<CardRuntimeState> handCards = new List<CardRuntimeState>();
            if (actingPlayer.handCards != null && actingPlayer.handCards.Count > 0)
            {
                handCards.AddRange(actingPlayer.handCards);
            }
            else if (computer.debugHandCards != null)
            {
                for (int i = 0; i < computer.debugHandCards.Count; i++)
                {
                    CardData cardData = computer.debugHandCards[i];
                    if (cardData == null)
                    {
                        continue;
                    }

                    handCards.Add(CardFactory.CreateRuntimeState(cardData));
                }
            }

            int currentTurn = 1;
            GamePhase currentPhase = gameManager.currentPhase;

            return new ComputerGameSnapshot(
                gameManager,
                hexGrid,
                currentPhase,
                currentTurn,
                actingPlayer,
                opponentPlayer,
                actingPlayerKey,
                opponentPlayerKey,
                handCards,
                new HexGridBoardStateReader(hexGrid));
        }

        private static PlayerState ResolveActingPlayer(GameManager gameManager, ComputerPlayer computer)
        {
            if (gameManager.player2 != null)
            {
                computer.playerState = gameManager.player2;
                return gameManager.player2;
            }

            return computer.playerState;
        }

        private static PlayerState ResolveOpponent(GameManager gameManager, PlayerState actingPlayer)
        {
            if (gameManager.player1 == null || gameManager.player2 == null)
            {
                return null;
            }

            return ReferenceEquals(gameManager.player1, actingPlayer)
                ? gameManager.player2
                : gameManager.player1;
        }

        private static string ResolvePlayerKey(GameManager gameManager, PlayerState state, string fallback)
        {
            if (state == null)
            {
                return fallback;
            }

            if (ReferenceEquals(gameManager.player1, state))
            {
                return "player";
            }

            if (ReferenceEquals(gameManager.player2, state))
            {
                return "enemy";
            }

            if (!string.IsNullOrWhiteSpace(state.playerName) && state.playerName.ToLower().Contains("opponent"))
            {
                return "enemy";
            }

            return fallback;
        }
    }
}
