using UnityEngine;

[CreateAssetMenu(fileName = "BomberCard", menuName = "Cards/Special/Bomber")]
public class BomberCardData : CharacterCardData
{
    [Header("Bomber Balancing")]
    [Min(0)]
    public int damageAfterDroppingBomb = 2;

    [Header("Attack Range Extension")]
    [Min(0)]
    public int bonusAttackRange = 2;

    [Header("Visuals")]
    public ProjectileVisualSettings projectileVisuals;
}
