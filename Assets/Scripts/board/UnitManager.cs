using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    private Unit selectedUnit;
    private List<HexTile> highlightedTiles = new List<HexTile>();
    private HexGrid grid;

    void Start()
    {
        grid = FindFirstObjectByType<HexGrid>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                HexTile clickedTile = hit.collider.GetComponent<HexTile>();
                if (clickedTile == null) return;

                if (selectedUnit == null)
                {
                    if (clickedTile.tileType == "unit" && clickedTile.owner == "player")
                    {
                        SelectUnit(clickedTile);
                    }
                }
                else
                {
                    if (highlightedTiles.Contains(clickedTile) && clickedTile.IsEmpty())
                    {
                        MoveUnit(clickedTile);
                    }
                    else
                    {
                        DeselectUnit();
                    }
                }
            }
        }
    }

    void SelectUnit(HexTile tile)
    {
        selectedUnit = FindUnitOnTile(tile);
        if (selectedUnit == null) return;

        highlightedTiles = HexUtils.GetTilesInRange(tile, selectedUnit.moveRange, grid);
        foreach (HexTile t in highlightedTiles)
        {
            if (t.IsEmpty())
                t.Highlight(Color.green);
        }
    }

    void MoveUnit(HexTile targetTile)
    {
        selectedUnit.currentTile.RemoveUnit();
        selectedUnit.PlaceOnTile(targetTile);
        selectedUnit.transform.position = targetTile.transform.position;
        DeselectUnit();
    }

    void DeselectUnit()
    {
        foreach (HexTile t in highlightedTiles)
        {
            t.ResetColor();
        }
        highlightedTiles.Clear();
        selectedUnit = null;
    }

    Unit FindUnitOnTile(HexTile tile)
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit u in allUnits)
        {
            if (u.currentTile == tile)
                return u;
        }
        return null;
    }
}