// Rabie: "Extended AI action execution with card, movement, attack routing plus ownership checks before board actions run."
using UnityEngine;

namespace FortGame.Computer
{
    /// <summary>
    /// Applies the selected legal action to current game state.
    /// </summary>
    public sealed class ComputerActionExecutor
    {
        // Ali: Executes the AI chosen action through CardPlayService so AI and player card rules stay identical.
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

            if (!action.isLegalAction)
            {
                Debug.LogWarning($"[ComputerActionExecutor] Blocked illegal action: {action.actionName}. {action.validationReason}");
                return false;
            }

            if (action.type == ActionType.MoveUnit)
            {
                return TryExecuteMoveAction(action, snapshot);
            }

            if (action.type == ActionType.AttackUnit || action.type == ActionType.AttackFort)
            {
                return TryExecuteAttackAction(action, snapshot);
            }

            if (!IsCardPlayAction(action.type))
            {
                Debug.LogWarning($"[ComputerActionExecutor] Unsupported action type: {action.type}.");
                return false;
            }

            // Ali: card-play actions need a source card before calling CardPlayService.
            if (action.sourceCard == null)
            {
                Debug.LogWarning($"[ComputerActionExecutor] Invalid card action payload: {action.actionName}");
                return false;
            }

            CardPlayService cardPlayService = ResolveCardPlayService(snapshot);
            if (cardPlayService == null)
            {
                Debug.LogWarning("[ComputerActionExecutor] Missing CardPlayService. AI card play was blocked to avoid rule drift.");
                return false;
            }

            string actingPlayerId = string.IsNullOrWhiteSpace(action.actingPlayerId)
                ? snapshot.ActingPlayerKey
                : action.actingPlayerId;

            CardPlayResult playResult = cardPlayService.PlayCard(action.sourceCard, actingPlayerId, action.target);
            if (!playResult.Succeeded)
            {
                Debug.LogWarning($"[ComputerActionExecutor] CardPlayService failed {action.actionName}: {playResult.ReasonCode} - {playResult.Message}");
                return false;
            }

            return true;
        }

        private static bool TryExecuteMoveAction(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            if (action.actingUnit == null || action.destinationTile == null)
            {
                Debug.LogWarning($"[ComputerActionExecutor] Invalid move action payload: {action?.actionName}");
                return false;
            }

            if (!CanUseActingUnit(action, snapshot))
            {
                return false;
            }

            UnitManager unitManager = ResolveUnitManager();
            if (unitManager == null)
            {
                Debug.LogWarning("[ComputerActionExecutor] Missing UnitManager. AI movement was blocked to avoid rule drift.");
                return false;
            }

            bool moved = unitManager.TryMoveUnit(action.actingUnit, action.destinationTile);
            if (!moved)
            {
                Debug.LogWarning($"[ComputerActionExecutor] UnitManager rejected move action: {action.actionName}");
            }

            return moved;
        }

        private static bool TryExecuteAttackAction(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            if (action.actingUnit == null || action.targetTile == null)
            {
                Debug.LogWarning($"[ComputerActionExecutor] Invalid attack action payload: {action?.actionName}");
                return false;
            }

            if (!CanUseActingUnit(action, snapshot))
            {
                return false;
            }

            UnitManager unitManager = ResolveUnitManager();
            if (unitManager == null)
            {
                Debug.LogWarning("[ComputerActionExecutor] Missing UnitManager. AI attack was blocked to avoid rule drift.");
                return false;
            }

            bool attacked = unitManager.TryAttackTarget(action.actingUnit, action.targetTile);
            if (!attacked)
            {
                Debug.LogWarning($"[ComputerActionExecutor] UnitManager rejected attack action: {action.actionName}");
            }

            return attacked;
        }

        private static bool CanUseActingUnit(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            if (action?.actingUnit == null || snapshot == null)
            {
                return false;
            }

            string actingPlayerId = string.IsNullOrWhiteSpace(action.actingPlayerId)
                ? snapshot.ActingPlayerKey
                : action.actingPlayerId;

            if (string.IsNullOrWhiteSpace(actingPlayerId))
            {
                Debug.LogWarning($"[ComputerActionExecutor] Missing acting player id for board action: {action.actionName}");
                return false;
            }

            if (action.actingUnit.owner != actingPlayerId)
            {
                Debug.LogWarning($"[ComputerActionExecutor] Blocked board action for wrong owner. Action={action.actionName}, UnitOwner={action.actingUnit.owner}, ActingPlayer={actingPlayerId}");
                return false;
            }

            return true;
        }


        // Ali: finds the shared CardPlayService used by both player and AI card play.
        private static CardPlayService ResolveCardPlayService(ComputerGameSnapshot snapshot)
        {
            if (snapshot?.GameManager != null)
            {
                CardPlayService serviceOnGameManager = snapshot.GameManager.GetComponent<CardPlayService>();
                if (serviceOnGameManager != null)
                {
                    return serviceOnGameManager;
                }
            }

            return Object.FindFirstObjectByType<CardPlayService>();
        }

        private static UnitManager ResolveUnitManager()
        {
            return Object.FindFirstObjectByType<UnitManager>();
        }


        // Ali: this helper filters the actions that must go through CardPlayService.
        private static bool IsCardPlayAction(ActionType actionType)
        {
            return actionType == ActionType.PlayUnitCard
                || actionType == ActionType.PlayWorldEffectCard
                || actionType == ActionType.PlaySpellCard;
        }


    }
}
