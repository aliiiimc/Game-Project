using UnityEngine;

[CreateAssetMenu(fileName = "BomberCard", menuName = "Cards/Special/Bomber")]
public class BomberCardData : CharacterCardData
{
    [Header("Bomber Balancing")]
    [Min(0)]
    public int damageAfterDroppingBomb = 2;

    [Header("Visuals")]
    public ProjectileVisualSettings projectileVisuals;
}
