using UnityEngine;
using System.Collections.Generic;

public class HexTile : MonoBehaviour
{
    public AxialCoord coord;
    public string tileType = "empty"; // empty, fort, unit
    public string owner = "none";     // none, player, enemy
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        originalColor = spriteRenderer.color;
    }

    public bool IsEmpty()
    {
        return tileType == "empty";
    }

    public void SetAsFort(Color fortColor, string fortOwner)
    {
        tileType = "fort";
        owner = fortOwner;
        originalColor = fortColor;
        spriteRenderer.color = fortColor;
    }

    public void PlaceUnit(string unitOwner)
    {
        tileType = "unit";
        owner = unitOwner;
        originalColor = (unitOwner == "player") ? new Color(0.2f, 0.4f, 1f) : new Color(1f, 0.3f, 0.3f);
        spriteRenderer.color = originalColor;
    }

    public void RemoveUnit()
    {
        tileType = "empty";
        owner = "none";
        originalColor = (coord.r < FindFirstObjectByType<HexGrid>().gridHeight / 2)
            ? new Color(0.6f, 0.8f, 1f)
            : new Color(1f, 0.7f, 0.7f);
        spriteRenderer.color = originalColor;
    }

    public void Highlight(Color color)
    {
        spriteRenderer.color = color;
    }

    public void ResetColor()
    {
        spriteRenderer.color = originalColor;
    }

    void OnMouseDown()
    {
        Debug.Log($"Clicked ({coord.q},{coord.r}) | Type: {tileType} | Owner: {owner}");

        FortGame.UI.TargetSelectionManager targetMgr = FortGame.UI.TargetSelectionManager.Instance;
        if (targetMgr != null)
        {
            targetMgr.TrySelectTarget(this);
        }
    }

    void OnMouseEnter()
    {
        if (tileType != "fort")
            spriteRenderer.color = Color.cyan;
    }

    void OnMouseExit()
    {
        spriteRenderer.color = originalColor;
    }
}
