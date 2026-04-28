using UnityEngine;

namespace FortGame.Computer
{
    /// <summary>
    /// Applies the selected legal action to current game state.
    /// </summary>
    public sealed class ComputerActionExecutor
    {
        public bool TryExecuteAction(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            if (action == null || snapshot == null || snapshot.ActingPlayer == null)
            {
                return false;
            }

            if (action.endsTurn || action.type == ActionType.EndTurn)
            {
                return false;
            }

            int actionCost = action.cost;
            if (snapshot.ActingPlayer.money < actionCost)
            {
                Debug.LogWarning($"[ComputerActionExecutor] Cannot execute {action.actionName}. Not enough money.");
                return false;
            }

            // Rabie: execute first, then spend money/remove the card only if the action worked.
            bool succeeded;
            switch (action.type)
            {
                case ActionType.PlayUnitCard:
                case ActionType.PlayWorldEffectCard:
                    succeeded = ExecutePlacementAction(action, snapshot);
                    break;

                case ActionType.PlaySpellCard:
                    succeeded = ExecuteSpellAction(action, snapshot);
                    break;

                default:
                    Debug.LogWarning($"[ComputerActionExecutor] Unsupported action type: {action.type}");
                    return false;
            }

            if (!succeeded)
            {
                return false;
            }

            snapshot.ActingPlayer.money -= actionCost;
            if (action.sourceCard != null && snapshot.ActingPlayer.handCards != null)
            {
                snapshot.ActingPlayer.handCards.Remove(action.sourceCard);
                if (!action.sourceCard.IsManifestedOnBoard)
                {
                    action.sourceCard.MoveToZone(CardZone.Discard);
                }

                if (snapshot.GameManager != null && ReferenceEquals(snapshot.GameManager.currentPlayer, snapshot.ActingPlayer) && snapshot.GameManager.handUI != null)
                {
                    snapshot.GameManager.handUI.RemoveCardFromHand(action.sourceCard);
                }
            }

            snapshot.ActingPlayer.handCount = snapshot.ActingPlayer.handCards != null
                ? snapshot.ActingPlayer.handCards.Count
                : Mathf.Max(0, snapshot.ActingPlayer.handCount - 1);

            return true;
        }

        private static bool ExecutePlacementAction(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            HexTile tile = snapshot.HexGrid.GetTile(action.target.tile);
            if (tile == null || !tile.IsEmpty())
            {
                return false;
            }

            if (action.type == ActionType.PlayWorldEffectCard)
            {
                // Rabie: world effects mark the tile as an effect, not as a normal unit.
                tile.PlaceWorldEffect(snapshot.ActingPlayerKey);
                action.sourceCard?.ManifestOnBoard(action.target.tile);
                return true;
            }

            tile.PlaceUnit(snapshot.ActingPlayerKey);
            action.sourceCard?.ManifestOnBoard(action.target.tile);
            return true;
        }

        private static bool ExecuteSpellAction(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            if (snapshot.GameManager == null)
            {
                return false;
            }

            if (action.target.type == CardTargetType.EnemyFort)
            {
                if (ReferenceEquals(snapshot.OpponentPlayer, snapshot.GameManager.player1))
                {
                    snapshot.GameManager.DamagePlayer1Fort(1);
                    return true;
                }

                if (ReferenceEquals(snapshot.OpponentPlayer, snapshot.GameManager.player2))
                {
                    snapshot.GameManager.DamagePlayer2Fort(1);
                    return true;
                }
            }

            return false;
        }
    }
}
