using System.Collections.Generic;
using UnityEngine;

namespace FortGame.Computer 
{
    /// <summary>
    /// Evaluates a list of possible ComputerActions and assigns each a score based on heuristic rules.
    /// </summary>
    public class ActionScoringSystem
    {
        /// <summary>
        /// Analyzes a list of actions and returns the one with the highest score.
        /// </summary>
        public ComputerAction GetBestAction(List<ComputerAction> possibleActions, PlayerState myState, int currentTurn)
        {
            ComputerAction bestAction = null;
            float highestScore = float.MinValue;

            foreach (var action in possibleActions)
            {
                float score = CalculateScore(action, myState, currentTurn);
                
                Debug.Log($"Action [{action.actionName}] scored: {score}");

                if (score > highestScore)
                {
                    highestScore = score;
                    bestAction = action;
                }
            }

            return bestAction;
        }

        private float CalculateScore(ComputerAction action, PlayerState myState, int currentTurn)
        {
            float score = 0f;

            // 1. Guarantee Win (+10000)
            if (action.willDestroyEnemyFort)
            {
                score += 10000f;
                // We can return immediately because nothing overrides a guaranteed win.
                return score; 
            }

            // 2. Save My Fort (Defensive priority)
            if (myState.fortHp < 5 && action.isDefensiveMove)
            {
                score += 500f; // High priority if dying
            }
            else if (action.isDefensiveMove)
            {
                // Minor points for defending if not in critical danger, just to be safe
                score += 20f;
            }

            // 3. Favorable Unit Trades (+100 to +300)
            if (action.type == ActionType.AttackUnit || action.type == ActionType.PlaySpellCard)
            {
                if (action.destroysEnemyUnit && action.survivesTrade)
                {
                    score += 300f; // Perfect trade
                }
                else if (action.destroysEnemyUnit && !action.survivesTrade)
                {
                    score += 100f; // Even trade
                }
                else if (!action.destroysEnemyUnit && !action.survivesTrade)
                {
                    score -= 100f; // Bad trade, we die for nothing
                }
            }

            // 4. Board Control / Pushing Forward
            if (action.movesCloserToEnemyFort)
            {
                score += 50f;
            }
            if (action.movesBackward)
            {
                score -= 50f;
            }

            // 5. Economy & Tempo Scaling
            // Early game cards are great early, terrible late.
            if (action.isEarlyGameCard)
            {
                if (currentTurn < 5) score += 60f;
                else score -= 30f; // Don't waste time on small cards late game
            }
            // Late game cards are great late, or if we can ramp to them.
            if (action.isLateGameCard)
            {
                if (currentTurn >= 5) score += 80f;
                else score -= 50f; // Too expensive to play early, or holds up tempo
            }

            // --- ADVANCED RULES ---

            // 6. Card Advantage
            if (action.drawsCards > 0)
            {
                // Base score for drawing cards
                score += action.drawsCards * 30f;
                
                // Desperation multiplier: If we are almost out of cards, drawing is incredibly valuable
                if (myState.handCount <= 2)
                {
                    score += action.drawsCards * 50f; 
                }
            }
            if (action.forcesDiscard > 0)
            {
                score += action.forcesDiscard * 40f;
            }

            // 7. Resource Efficiency
            // Reward actions that utilize more of our available mana/money 
            // (Assumes we already validated we have enough money to play it)
            if (action.cost > 0)
            {
                score += action.cost * 15f; 
            }

            // 8. Synergies and Combos
            if (action.hasSynergyOnBoard)
            {
                score += 75f;
            }

            return score;
        }
    }
}
