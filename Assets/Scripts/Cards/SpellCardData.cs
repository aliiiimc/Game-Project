// Data model for spell cards that define offensive/defensive effects with power, duration, and optional movement.
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

    public OptionalInt spellMovementCapacity;

    public override OptionalInt MovementCapacity => spellMovementCapacity;
}
