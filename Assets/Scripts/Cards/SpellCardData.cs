using UnityEngine;

[CreateAssetMenu(fileName = "SpellCard", menuName = "Cards/Spell")]
public class SpellCardData : CardData
{
    [Header("Spell")]
    // Main spell category used by the effect resolver.
    public SpellEffectType effectType;

    // Numeric intensity of the effect (damage amount, buff value, etc.).
    public int effectPower;

    // Optional duration in turns for persistent effects. Set 0 for instant effects.
    public int effectDurationTurns;

    // Optional movement value for spells that reposition or grant movement.
    public OptionalInt spellMovementCapacity;

    // Spell cards expose their own movement-related value.
    public override OptionalInt MovementCapacity => spellMovementCapacity;
}
