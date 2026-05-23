using UnityEngine;

public class Miner : SpecialCardScriptBase
{
    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return CardNameMatches(unitCardData, "Miner");
    }

    public override bool ConsumeMoveAction(Unit unit, CharacterCardData unitCardData)
    {
        return true;
    }

    public override void OnBeforeMove(Unit unit, CharacterCardData unitCardData)
    {
        float movingVisibilityAlpha = 0.3f;
        if (unitCardData is MinerCardData minerCardData)
        {
            movingVisibilityAlpha = Mathf.Clamp01(minerCardData.movingVisibilityAlpha);
        }

        SetAlpha(unit, movingVisibilityAlpha);
    }

    public override void OnAfterMove(Unit unit, CharacterCardData unitCardData, HexTile destinationTile)
    {
        SetAlpha(unit, 1f);
    }

    private static void SetAlpha(Unit unit, float alpha)
    {
        if (unit == null)
        {
            return;
        }

        SpriteRenderer renderer = unit.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }
}
