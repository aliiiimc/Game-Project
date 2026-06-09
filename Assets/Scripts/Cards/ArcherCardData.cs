using UnityEngine;

[CreateAssetMenu(fileName = "ArcherCard", menuName = "Cards/Special/Archer")]
public class ArcherCardData : CharacterCardData
{
    [Header("Archer Visuals")]
    public ProjectileVisualSettings projectileVisuals = new ProjectileVisualSettings();
}
