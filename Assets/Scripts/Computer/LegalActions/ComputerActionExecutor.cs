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

            snapshot.ActingPlayer.money -= actionCost;
            if (snapshot.ActingPlayer.handCount > 0)
            {
                snapshot.ActingPlayer.handCount -= 1;
            }

            switch (action.type)
            {
                case ActionType.PlayUnitCard:
                case ActionType.PlayWorldEffectCard:
                    return ExecutePlacementAction(action, snapshot);

                case ActionType.PlaySpellCard:
                    return ExecuteSpellAction(action, snapshot);

                default:
                    Debug.LogWarning($"[ComputerActionExecutor] Unsupported action type: {action.type}");
                    return false;
            }
        }

        private static bool ExecutePlacementAction(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            HexTile tile = snapshot.HexGrid.GetTile(action.target.tile);
            if (tile == null || !tile.IsEmpty())
            {
                return false;
            }

            tile.PlaceUnit(snapshot.ActingPlayerKey);
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
