using UnityEngine;

namespace FortGame.Computer 
{
    public enum ActionType
    {
        PlayUnitCard,
        PlaySpellCard,
        MoveUnit,
        AttackUnit,
        AttackFort
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

        public ComputerAction(string name, ActionType t)
        {
            actionName = name;
            type = t;
        }
    }
}
