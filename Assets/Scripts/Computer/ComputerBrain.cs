using System.Collections.Generic;
using UnityEngine;

namespace FortGame.Computer
{
    // Rabie : "Changed AI decision execution so it retries legal actions and prioritizes a moved unit's same-turn follow-up attack."
    /// <summary>
    /// The logic center for the ComputerPlayer. It evaluates the current game state
    /// and decides the best available action.
    /// </summary>
    public class ComputerBrain
    {
        private readonly ActionScoringSystem _scoringSystem;
        private readonly ComputerGameSnapshotProvider _snapshotProvider;
        private readonly LegalActionGenerator _legalActionGenerator;
        private readonly ComputerActionExecutor _actionExecutor;
        private Unit _preferredFollowUpAttacker;
        private int _preferredFollowUpRound = -1;

        public ComputerBrain()
        {
            _scoringSystem = new ActionScoringSystem();
            _snapshotProvider = new ComputerGameSnapshotProvider();
            _legalActionGenerator = new LegalActionGenerator(new BasicComputerTargetValidator());
            _actionExecutor = new ComputerActionExecutor();
        }

        /// <summary>
        /// Analyzes the board and the AI's internal state to find the next move.
        /// Returns true if an action was chosen and executed; false if no actions are left.
        /// </summary>
        /// <param name="computer">The AI component owning this brain.</param>
        public bool DetermineNextAction(ComputerPlayer computer)
        {
            if (computer == null)
            {
                return false;
            }

            string playerName = computer.playerState != null ? computer.playerState.playerName : "Computer";
            Debug.Log($"[ComputerBrain] Evaluating actions for {playerName}");


            ComputerGameSnapshot snapshot = _snapshotProvider.CreateSnapshot(computer);
            if (snapshot == null)
            {
                Debug.LogWarning("[ComputerBrain] Snapshot creation failed. Ending turn safely.");
                return false;
            }

            // 1. Gather all legally possible actions.
            List<ComputerAction> possibleActions = GeneratePossibleActions(snapshot);

            Debug.Log($"[ComputerBrain] Legal actions found: {possibleActions?.Count ?? 0}"); //Ali: prevents the log from crashing if the actions list is ever null.

            if (_legalActionGenerator.Diagnostics != null)
            {
                _legalActionGenerator.Diagnostics.LogSummary();
            }

            // Ali: if there are no generated legal actions, the AI ends its turn.
            if (possibleActions == null || possibleActions.Count == 0)
            {
                return false;
            }

            // 2. Score the actions using the Utility AI system.
            List<ComputerAction> sortedActions = _scoringSystem.GetActionsByScoreDescending(possibleActions, snapshot.ActingPlayer, snapshot.CurrentTurn);
            if (sortedActions.Count == 0)
            {
                return false;
            }

            ComputerAction failedFollowUpAction = null;
            ComputerAction followUpAttack = FindPreferredFollowUpAttack(sortedActions, snapshot);
            if (followUpAttack != null)
            {
                Debug.Log($"[ComputerBrain] Prioritizing move-then-attack follow-up: {followUpAttack.actionName}");
                if (ExecuteAction(followUpAttack, snapshot))
                {
                    return true;
                }

                failedFollowUpAction = followUpAttack;
                ClearPreferredFollowUpAttack();
            }

            float bestScore = _scoringSystem.GetScore(sortedActions[0], snapshot.ActingPlayer, snapshot.CurrentTurn);
            if (bestScore <= 0f)
            {
                Debug.Log($"[ComputerBrain] Best action score is {bestScore}. Ending turn because no good legal action remains.");
                return false;
            }

            for (int i = 0; i < sortedActions.Count; i++)
            {
                ComputerAction candidateAction = sortedActions[i];
                if (candidateAction == null)
                {
                    continue;
                }

                if (ReferenceEquals(candidateAction, failedFollowUpAction))
                {
                    continue;
                }

                if (candidateAction.endsTurn || candidateAction.type == ActionType.EndTurn)
                {
                    Debug.Log("[ComputerBrain] Reached End Turn action after trying useful actions.");
                    return false;
                }

                if (ExecuteAction(candidateAction, snapshot))
                {
                    return true;
                }
            }

            Debug.LogWarning("[ComputerBrain] No scored action executed successfully. Ending turn safely.");
            return false;
        }


        // Ali: small wrapper so DetermineNextAction stays readable.
        // The real legal-action logic stays inside LegalActionGenerator.
        private List<ComputerAction> GeneratePossibleActions(ComputerGameSnapshot snapshot)
        {
            return _legalActionGenerator.GenerateLegalActions(snapshot);
        }



        // Ali: executes the selected action and reports whether it really changed game state.
        private bool ExecuteAction(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            Debug.Log($"[ComputerBrain] Chose to execute: {action.actionName}");

            bool success = _actionExecutor.TryExecuteAction(action, snapshot);
            if (!success)
            {
                Debug.LogWarning($"[ComputerBrain] Failed to execute action: {action.actionName}");
            }

            if (success)
            {
                UpdatePreferredFollowUpAttack(action, snapshot);
            }

            return success;
        }

        private ComputerAction FindPreferredFollowUpAttack(List<ComputerAction> sortedActions, ComputerGameSnapshot snapshot)
        {
            if (_preferredFollowUpAttacker == null || snapshot == null)
            {
                return null;
            }

            if (_preferredFollowUpRound != snapshot.CurrentTurn
                || _preferredFollowUpAttacker.owner != snapshot.ActingPlayerKey
                || !_preferredFollowUpAttacker.CanAttack())
            {
                ClearPreferredFollowUpAttack();
                return null;
            }

            for (int i = 0; i < sortedActions.Count; i++)
            {
                ComputerAction action = sortedActions[i];
                if (action == null || !IsAttackAction(action))
                {
                    continue;
                }

                if (ReferenceEquals(action.actingUnit, _preferredFollowUpAttacker))
                {
                    return action;
                }
            }

            ClearPreferredFollowUpAttack();
            return null;
        }

        private void UpdatePreferredFollowUpAttack(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            if (action == null || snapshot == null)
            {
                ClearPreferredFollowUpAttack();
                return;
            }

            if (IsAttackAction(action))
            {
                if (ReferenceEquals(action.actingUnit, _preferredFollowUpAttacker))
                {
                    ClearPreferredFollowUpAttack();
                }

                return;
            }

            if (action.type != ActionType.MoveUnit || !MoveCreatedAttackOpportunity(action, snapshot))
            {
                return;
            }

            _preferredFollowUpAttacker = action.actingUnit;
            _preferredFollowUpRound = snapshot.CurrentTurn;
            Debug.Log($"[ComputerBrain] Stored move-then-attack follow-up for {action.actingUnit.name}.");
        }

        private static bool MoveCreatedAttackOpportunity(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            if (action?.actingUnit == null || snapshot == null || action.actingUnit.owner != snapshot.ActingPlayerKey)
            {
                return false;
            }

            if (!action.actingUnit.CanAttack())
            {
                return false;
            }

            UnitManager unitManager = Object.FindFirstObjectByType<UnitManager>();
            if (unitManager == null)
            {
                return false;
            }

            List<HexTile> targetTiles = unitManager.GetLegalAttackTargets(action.actingUnit);
            for (int i = 0; i < targetTiles.Count; i++)
            {
                HexTile targetTile = targetTiles[i];
                if (targetTile == null)
                {
                    continue;
                }

                if ((targetTile.tileType == "unit" || targetTile.tileType == "fort")
                    && targetTile.owner == snapshot.OpponentPlayerKey)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearPreferredFollowUpAttack()
        {
            _preferredFollowUpAttacker = null;
            _preferredFollowUpRound = -1;
        }

        private static bool IsAttackAction(ComputerAction action)
        {
            return action != null
                && (action.type == ActionType.AttackUnit || action.type == ActionType.AttackFort);
        }
    }
}
