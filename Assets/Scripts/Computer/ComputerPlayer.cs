using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FortGame.Computer 
{
    /// <summary>
    /// Represents the Computer opponent in the game.
    /// Manages the state and orchestrates the turn loop by delegating decisions to the ComputerBrain.
    /// </summary>
    public class ComputerPlayer : MonoBehaviour
    {
        [Header("Player Data")]
        public PlayerState playerState;

        [Header("Computer Settings")]
        public float delayBetweenActions = 1.5f; // Wait time to simulate thinking and allow user to observe

        private ComputerBrain _brain;
        private bool _isMyTurn = false;

        private void Awake()
        {
            // Initialize the Computer Brain
            _brain = new ComputerBrain();

            // Setup initial player state if not provided
            if (playerState == null)
            {
                playerState = new PlayerState()
                {
                    playerName = "Opponent (AI)",
                    money = 5,
                    fortHp = 20,
                    maxHandSize = 5
                };
            }
        }

        /// <summary>
        /// Called by the Game Flow Manager when it's the Computer's turn.
        /// </summary>
        public void StartTurn()
        {
            if (_isMyTurn) return;
            
            _isMyTurn = true;
            Debug.Log($"[{playerState.playerName}] Turn Started!");
            
            // Begin the Computer action loop
            StartCoroutine(TurnLoopRoutine());
        }

        private IEnumerator TurnLoopRoutine()
        {
            // Initial thinking delay
            yield return new WaitForSeconds(delayBetweenActions);

            int loopSecurity = 0; // Prevent infinite loops
            
            while (_isMyTurn && loopSecurity < 20)
            {
                loopSecurity++;

                // 1. Brain analyzes the board and player state to find the best action
                bool hasActionToTake = _brain.DetermineNextAction(playerState);

                if (hasActionToTake)
                {
                    // Execute the action (we will flesh this out with ActionScoringSystem)
                    Debug.Log($"[{playerState.playerName}] Executing chosen action...");
                    
                    // Simulate resource/money exhaustion for now
                    playerState.money--; 

                    // Wait so the human player can see what just happened
                    yield return new WaitForSeconds(delayBetweenActions);
                }
                else
                {
                    // No valid actions left (e.g., out of money or cards), so end turn
                    Debug.Log($"[{playerState.playerName}] Has no more valid actions.");
                    break;
                }
            }

            EndTurn();
        }

        private void EndTurn()
        {
            _isMyTurn = false;
            Debug.Log($"[{playerState.playerName}] Ended Turn.");
            
            // TODO: Notify the Game Flow Manager that our turn is complete.
            // GameFlowManager.Instance.EndTurn(this);
        }
    }
}
