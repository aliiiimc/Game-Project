using UnityEngine;

[CreateAssetMenu(fileName = "SpellCard", menuName = "Cards/Spell")]
public class SpellCardData : CardData
{
    [Header("Spell")]
    public SpellEffectType effectType;

    public int effectPower;

    public int effectDurationTurns;

    public OptionalInt spellMovementCapacity;

    public override OptionalInt MovementCapacity => spellMovementCapacity;
}
