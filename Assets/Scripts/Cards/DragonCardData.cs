using UnityEngine;

[CreateAssetMenu(fileName = "DragonCard", menuName = "Cards/Special/Dragon")]
public class DragonCardData : CharacterCardData
{
    [Header("Dragon Visuals")]
    public ProjectileVisualSettings projectileVisuals = new ProjectileVisualSettings();
}
