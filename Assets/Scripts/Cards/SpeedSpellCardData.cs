using UnityEngine;

[CreateAssetMenu(fileName = "SpeedSpellCard", menuName = "Cards/Special/Speed Spell")]
public class SpeedSpellCardData : SpellCardData
{
    [Header("Speed Spell Balancing")]
    public int movementCapacityBonus = 2;
}
