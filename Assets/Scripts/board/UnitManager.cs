using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    private Unit selectedUnit;
    private List<HexTile> moveTiles = new List<HexTile>();
    private List<HexTile> attackTiles = new List<HexTile>();
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

            if (hit.collider == null) return;

            HexTile clickedTile = hit.collider.GetComponent<HexTile>();
            if (clickedTile == null) return;

            if (selectedUnit == null)
            {
                // Select a player unit
                if (clickedTile.tileType == "unit" && clickedTile.owner == "player")
                {
                    SelectUnit(clickedTile);
                }
            }
            else
            {
                // Attack an enemy in attack range
                if (attackTiles.Contains(clickedTile) && clickedTile.owner == "enemy")
                {
                    AttackTarget(clickedTile);
                }
                // Move to an empty tile in move range
                else if (moveTiles.Contains(clickedTile) && clickedTile.IsEmpty())
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

    void SelectUnit(HexTile tile)
    {
        selectedUnit = FindUnitOnTile(tile);
        if (selectedUnit == null) return;

        // Movement range (green)
        moveTiles = HexUtils.GetTilesInRange(tile, selectedUnit.moveRange, grid);
        foreach (HexTile t in moveTiles)
        {
            if (t.IsEmpty())
                t.Highlight(Color.green);
        }

        // Attack range (red) — highlight enemies within attackRange
        attackTiles = HexUtils.GetTilesInRange(tile, selectedUnit.attackRange, grid);
        foreach (HexTile t in attackTiles)
        {
            if (t.owner == "enemy")
                t.Highlight(Color.red);
        }
    }

    void MoveUnit(HexTile targetTile)
    {
        selectedUnit.currentTile.RemoveUnit();
        selectedUnit.PlaceOnTile(targetTile);
        selectedUnit.transform.position = targetTile.transform.position;
        DeselectUnit();
    }

    void AttackTarget(HexTile targetTile)
    {
        Unit target = FindUnitOnTile(targetTile);
        if (target != null)
        {
            target.health -= selectedUnit.attack;
            Debug.Log($"Attacked! Target health: {target.health}");

            if (target.health <= 0)
            {
                Debug.Log("Target died!");
                target.Die();
            }
        }
        else if (targetTile.tileType == "fort")
        {
            Debug.Log($"Attacked enemy fort!");
            // Fort damage logic will go here later
        }

        DeselectUnit();
    }

    void DeselectUnit()
    {
        foreach (HexTile t in moveTiles) t.ResetColor();
        foreach (HexTile t in attackTiles) t.ResetColor();
        moveTiles.Clear();
        attackTiles.Clear();
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