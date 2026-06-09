using UnityEngine;

[CreateAssetMenu(fileName = "AntiAirTowerCard", menuName = "Cards/Special/Anti-Air Tower")]
public class AntiAirTowerCardData : WorldEffectCardData
{
    [Header("Anti-Air Tower Visuals")]
    public ProjectileVisualSettings projectileVisuals = new ProjectileVisualSettings();
}
