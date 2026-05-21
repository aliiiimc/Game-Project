using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCard", menuName = "Cards/Character")]
public class CharacterCardData : CardData
{
    [Header("Character")]
    public Sprite manifestedSprite;

    public int maxHp;

    public int attackDamage;

    public int attackRange = 1;

    public AttackType attackType = AttackType.Melee;

    public AttackTarget attackTarget = AttackTarget.Ground;

    public MovementType movementType = MovementType.Ground;

    public bool startsReadyToAttack;

    public OptionalInt unitMovementCapacity;

    public override OptionalInt MovementCapacity => unitMovementCapacity;

    // Ali: allows special characters (like European King) to capture enemy world effects.
    public bool canColonizeEnemyWorldEffects;

}
