using UnityEngine;

[CreateAssetMenu(fileName = "WheatFieldCard", menuName = "Cards/Special/Wheat Field")]
public class WheatFieldCardData : WorldEffectCardData
{
    [Header("Wheat Field Balancing")]
    public int tilesPerField = 6;
    public int hpPerTile = 1;
    public int bonusMoneyPerTurn = 1;

    public int GetTotalClusterHp()
    {
        return structureHp.HasValue ? structureHp.Value : (Mathf.Max(1, tilesPerField) * Mathf.Max(1, hpPerTile));
    }
}
