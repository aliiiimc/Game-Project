using UnityEngine;

[CreateAssetMenu(fileName = "WorldEffectCard", menuName = "Cards/World Effect")]
public class WorldEffectCardData : CardData
{
    [Header("World Effect")]
    public Sprite manifestedSprite;

    public WorldEffectCategory category;

    public OptionalInt structureHp;

    public OptionalInt structureDamage;

    public OptionalInt worldEffectAttackRange;

    public AttackType attackType = AttackType.Projectile;

    public AttackTarget attackTarget = AttackTarget.Ground;

    public OptionalInt revenuePerTurn;

    public OptionalInt visionModifier;

    public OptionalInt movementModifier;

    public int durationTurns;

    public OptionalInt worldEffectMovementCapacity;

    public override OptionalInt MovementCapacity => worldEffectMovementCapacity;
}
