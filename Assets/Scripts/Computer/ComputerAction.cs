// Rabie: "Added unit action payload fields so AI movement and attack actions can remember their unit and board tile."
using UnityEngine;

namespace FortGame.Computer 
{
    public enum ActionType
    {
        PlayUnitCard,
        PlayWorldEffectCard,
        PlaySpellCard,
        MoveUnit,
        AttackUnit,
        AttackFort,
        EndTurn
    }

    /// <summary>
    /// Represents a possible move the Computer can make.
    /// Because the actual Card and Board classes are not finished yet by the team,
    /// this class contains "stub" simulation properties to test our scoring system.
    /// </summary>
    public class ComputerAction
    {
        public string actionName;
        public ActionType type;

        // Execution payload used by the legal-action reader and executor.
        public string actingPlayerId;
        public CardRuntimeState sourceCard;
        public CardRuntimeState auxiliaryCard;
        public CardTarget target;
        public Unit actingUnit;
        public HexTile destinationTile;
        public HexTile targetTile;
        public string sourceCardName;
        public string validationReason;
        public bool isLegalAction = true;
        public bool isGeneratedByLegalReader = false;

        // Properties needed for the 5 Rules we laid out:
        
        [Header("Rule 1: Guarantee Win")]
        public bool willDestroyEnemyFort = false;

        [Header("Rule 2: Save My Fort")]
        public bool isDefensiveMove = false; // Does it protect our fort?

        [Header("Rule 3: Unit Trades")]
        public bool survivesTrade = false;
        public bool destroysEnemyUnit = false;

        [Header("Rule 4: Board Control")]
        public bool movesCloserToEnemyFort = false;
        public bool movesBackward = false;

        [Header("Rule 5: Tempo")]
        public bool isEarlyGameCard = false;
        public bool isLateGameCard = false;

        [Header("Advanced: Card Advantage")]
        public int drawsCards = 0;
        public int forcesDiscard = 0;

        [Header("Advanced: Resource Efficiency")]
        public int cost = 0;

        [Header("Advanced: Synergy")]
        public bool hasSynergyOnBoard = false;
        // (abdo :) Extra board-position score so the generator can break ties between legal actions intelligently.
        public float tacticalScore = 0f;

        [Header("Turn Control")]
        public bool endsTurn = false;

        public ComputerAction(string name, ActionType t)
        {
            actionName = name;
            type = t;
        }

        public static ComputerAction CreateEndTurnAction(string actingPlayer)
        {
            return new ComputerAction("End Turn", ActionType.EndTurn)
            {
                actingPlayerId = actingPlayer,
                endsTurn = true,
                isGeneratedByLegalReader = true,
                isLegalAction = true,
                cost = 0
            };
        }
    }
}
