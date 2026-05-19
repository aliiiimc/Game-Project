using UnityEngine;

[CreateAssetMenu(fileName = "EngineerCard", menuName = "Cards/Special/Engineer")]
public class EngineerCardData : CharacterCardData
{
    [Header("Engineer Balancing")]
    [Min(0)]
    public int structureRepairBoostAmount = 6;
}
