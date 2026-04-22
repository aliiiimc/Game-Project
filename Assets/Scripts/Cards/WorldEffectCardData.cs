using UnityEngine;

[CreateAssetMenu(fileName = "WorldEffectCard", menuName = "Cards/World Effect")]
public class WorldEffectCardData : CardData
{
    [Header("World Effect")]
    public Sprite manifestedSprite;

    public WorldEffectCategory category;

    public OptionalInt structureHp;

    public OptionalInt structureDamage;

    public OptionalInt revenuePerTurn;

    public OptionalInt visionModifier;

    public OptionalInt movementModifier;

    public int durationTurns;

    public OptionalInt worldEffectMovementCapacity;

    public override OptionalInt MovementCapacity => worldEffectMovementCapacity;
}