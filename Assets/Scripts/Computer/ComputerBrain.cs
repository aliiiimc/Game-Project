using System.Collections.Generic;
using UnityEngine;

namespace FortGame.Computer 
{
    /// <summary>
    /// The logic center for the ComputerPlayer. It evaluates the current game state
    /// and decides the best available action.
    /// </summary>
    public class ComputerBrain
    {
        private ActionScoringSystem _scoringSystem;

        public ComputerBrain()
        {
            _scoringSystem = new ActionScoringSystem();
        }

        /// <summary>
        /// Analyzes the board and the AI's internal state to find the next move.
        /// Returns true if an action was chosen and executed; false if no actions are left.
        /// </summary>
        /// <param name="state">The PlayerState to evaluate (e.g. current money, cards).</param>
        public bool DetermineNextAction(PlayerState state)
        {
            Debug.Log($"[ComputerBrain] Evaluating actions for {state.playerName} - Money Left: {state.money}");

            // 1. Gather all legally possible actions.
            List<ComputerAction> possibleActions = GeneratePossibleActions(state);

            // If we have no money or no possible moves, we can't do anything.
            if (possibleActions.Count == 0)
            {
                return false;
            }

            // 2. Score the actions using the Utility AI system
            // Note: For now we pass currentTurn=1 as a placeholder until GameFlowManager provides it.
            ComputerAction bestAction = _scoringSystem.GetBestAction(possibleActions, state, 1);

            if (bestAction != null)
            {
                // 3. Execute best action
                ExecuteAction(bestAction);
            }

            return true;
        }

        private List<ComputerAction> GeneratePossibleActions(PlayerState state)
        {
            List<ComputerAction> actions = new List<ComputerAction>();

            // Simulate logic: If we have money, we can "Play a Card"
            if (state.money > 0)
            {
                // Let's create a couple of fake actions to test our heuristics
                var attack = new ComputerAction("Play Heavy Knight", ActionType.PlayUnitCard);
                attack.willDestroyEnemyFort = true; // Simulating a lethal move

                var defend = new ComputerAction("Play Wall Shield", ActionType.PlaySpellCard);
                defend.isDefensiveMove = true; 

                actions.Add(attack);
                actions.Add(defend);
            }

            return actions;
        }

        private void ExecuteAction(ComputerAction action)
        {
            Debug.Log($"[ComputerBrain] Chose to execute: {action.actionName}");
            // In a real implementation, this would trigger the actual game logical commands:
            // e.g., BoardManager.Instance.PlaceCard(tile, card);
        }
    }
}
