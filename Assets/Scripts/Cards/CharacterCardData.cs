// Data model for character/unit cards with health, attack damage, readiness state, and board movement capacity.
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCard", menuName = "Cards/Character")]
public class CharacterCardData : CardData
{
    [Header("Character")]
    // Sprite used when this character is manifested on the board.
    public Sprite manifestedSprite;

    // Unit health when the card is manifested on board.
    public int maxHp;

    // Base damage dealt by this unit when it attacks.
    public int attackDamage;

    // Whether the unit can attack immediately after being summoned.
    public bool startsReadyToAttack;

    public OptionalInt unitMovementCapacity;

    public override OptionalInt MovementCapacity => unitMovementCapacity;
}
