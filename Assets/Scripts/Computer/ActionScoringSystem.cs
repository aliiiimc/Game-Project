using System.Collections.Generic;
using UnityEngine;

namespace FortGame.Computer
{
    // Rabie : "Added sorted AI scoring so the brain can try the next best action when the first scored action cannot execute."
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
            List<ComputerAction> sortedActions = GetActionsByScoreDescending(possibleActions, myState, currentTurn);
            return sortedActions.Count > 0 ? sortedActions[0] : null;
        }

        public List<ComputerAction> GetActionsByScoreDescending(List<ComputerAction> possibleActions, PlayerState myState, int currentTurn)
        {
            List<ScoredAction> scoredActions = new List<ScoredAction>();

            // Ali: simple protection if the generator returns no actions or a missing list.
            if (possibleActions == null || possibleActions.Count == 0)
            {
                return new List<ComputerAction>();
            }

            foreach (var action in possibleActions)
            {
                if (action == null)
                {
                    continue;
                }

                float score = CalculateScore(action, myState, currentTurn);

                Debug.Log($"[ActionScoringSystem] Action [{action.actionName}] scored: {score}");
                scoredActions.Add(new ScoredAction(action, score));
            }

            scoredActions.Sort((left, right) => right.Score.CompareTo(left.Score));

            List<ComputerAction> sortedActions = new List<ComputerAction>();
            for (int i = 0; i < scoredActions.Count; i++)
            {
                sortedActions.Add(scoredActions[i].Action);
            }

            return sortedActions;
        }

        public float GetScore(ComputerAction action, PlayerState myState, int currentTurn)
        {
            return CalculateScore(action, myState, currentTurn);
        }


        // Ali: scores one legal action. Higher score means the AI is more likely to choose it.
        private float CalculateScore(ComputerAction action, PlayerState myState, int currentTurn)
        {
            float score = 0f;
            if (action == null)
            {
                return float.MinValue;
            }


            if (action.endsTurn || action.type == ActionType.EndTurn)
            {
                // End-turn should only be selected when nothing useful is available.
                return -1000f;
            }

            // 1. Guarantee Win (+10000)
            if (action.willDestroyEnemyFort)
            {
                score += 10000f;
                // We can return immediately because nothing overrides a guaranteed win.
                return score;
            }

            // 2. Save My Fort (Defensive priority)

            if (myState != null && myState.fortHp < 5 && action.isDefensiveMove)  // Ali: avoids an error if myState is null.
            {
                score += 500f; // High priority if dying
            }
            else if (action.isDefensiveMove)
            {
                // Minor points for defending if not in critical danger, just to be safe
                score += 40f;
            }

            // 3. Favorable Unit Trades (+100 to +300)
            bool isUnitTradeAction = action.type == ActionType.AttackUnit
                || (action.type == ActionType.PlaySpellCard && action.target.type == CardTargetType.EnemyUnit);

            if (isUnitTradeAction)
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
                // Ali: Changed what was here before, so now if the AI fort is low, going forward is still possible, but less important than defending.
                score += myState != null && myState.fortHp < 8 ? 15f : 50f;
            }

            if (action.hasSynergyOnBoard)
            {
                score += 75f;
            }

            // (abdo :) Apply the tile/movement score added by the legal-action generator.
            score += action.tacticalScore;

            if (action.movesBackward)
            {
                score -= 50f;
            }

            // 5. Economy & Tempo Scaling
            // Early game cards are great early, terrible late.
            if (action.isEarlyGameCard)
            {
                score += currentTurn < 5 ? 60f : -30f;
            }

            // Late game cards are great in late game, or if we can ramp to them.
            if (action.isLateGameCard)
            {
                score += currentTurn >= 5 ? 80f : -50f;
            }


            // --- ADVANCED RULES ---

            // 6. Card Advantage
            if (action.drawsCards > 0)
            {
                // Base score for drawing cards
                score += action.drawsCards * 30f;

                // Desperation multiplier: If we are almost out of cards, drawing is incredibly valuable
                if (myState != null && myState.handCount <= 2)
                {
                    score += action.drawsCards * 50f;
                }
            }
            if (action.forcesDiscard > 0)
            {
                score += action.forcesDiscard * 40f;
            }

            // 7. Play cost
            // Ali: playing cards is free in the current rules, so action.cost intentionally does not affect AI score.


            return score;
        }

        private struct ScoredAction
        {
            public ScoredAction(ComputerAction action, float score)
            {
                Action = action;
                Score = score;
            }

            public ComputerAction Action { get; }
            public float Score { get; }
        }
    }
}
