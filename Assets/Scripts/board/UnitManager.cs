using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public float smoothMoveDuration = 0.25f;
    public float walkBobHeight = 0.08f;
    public float walkLeanAngle = 4f;
    public float walkSquashAmount = 0.08f;

    private Unit selectedUnit;
    private List<HexTile> moveTiles = new List<HexTile>();
    private List<HexTile> attackTiles = new List<HexTile>();
    private HexGrid grid;

    private GameManager gameManager; //Ali
    private string lastActiveOwner = "";
    private bool isAnimatingUnit;


    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>(); //Def de GameManager
        grid = FindFirstObjectByType<HexGrid>();
        ResetUnitsForActiveOwnerIfNeeded();
    }


    void Update()
    {
        ResetUnitsForActiveOwnerIfNeeded();

        if (isAnimatingUnit)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider == null) return;

            HexTile clickedTile = hit.collider.GetComponent<HexTile>();
            if (clickedTile == null) return;

            if (selectedUnit == null)
            {
                // Select one of the current player's units.
                if (clickedTile.tileType == "unit" && clickedTile.owner == GetActiveOwner())
                {
                    SelectUnit(clickedTile);
                }
            }
            else
            {
                // Attack an enemy in attack range
                if (attackTiles.Contains(clickedTile) && IsEnemyTarget(clickedTile))
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
        if (selectedUnit.CanMove())
        {
            moveTiles = HexUtils.GetReachableMoveTiles(tile, selectedUnit.moveRange, grid);
            foreach (HexTile t in moveTiles)
            {
                t.Highlight(Color.green);
            }
        }

        // Attack range (red) — highlight enemies within attackRange
        if (selectedUnit.CanAttack())
        {
            attackTiles = HexUtils.GetTilesInRange(tile, selectedUnit.attackRange, grid);
            foreach (HexTile t in attackTiles)
            {
                if (IsEnemyTarget(t))
                    t.Highlight(Color.red);
            }
        }
    }

    void MoveUnit(HexTile targetTile)
    {
        if (selectedUnit == null || !selectedUnit.CanMove())
        {
            DeselectUnit();
            return;
        }

        Unit movingUnit = selectedUnit;
        Vector3 startPosition = movingUnit.transform.position;
        Vector3 targetPosition = targetTile.transform.position;

        movingUnit.currentTile.RemoveUnit();
        movingUnit.PlaceOnTile(targetTile, snapToTile: false);
        movingUnit.MarkMoved();
        DeselectUnit();

        StartCoroutine(MoveUnitSmoothly(movingUnit, startPosition, targetPosition));
    }

    void AttackTarget(HexTile targetTile)
    {
        if (selectedUnit == null || !selectedUnit.CanAttack())
        {
            DeselectUnit();
            return;
        }

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
        else if (targetTile.tileType == "fort") //Ali : Update de AttackTarget
        {
            Debug.Log("Attacked fort!");

            if (gameManager == null)
            {
                Debug.LogWarning("GameManager not found. Cannot damage fort.");
                return;
            }

            if (targetTile.owner == "enemy")
            {
                gameManager.DamagePlayer2Fort(selectedUnit.attack);
            }
            else if (targetTile.owner == "player")
            {
                gameManager.DamagePlayer1Fort(selectedUnit.attack);
            }
        }
        // Ali: colonization rule - special units can convert enemy world effects instead of dealing damage.
        else if (targetTile.tileType == "worldEffect"
                 && selectedUnit.canColonizeEnemyWorldEffects
                 && targetTile.owner != GetActiveOwner())
        {
            targetTile.PlaceWorldEffect(GetActiveOwner());
            Debug.Log("Colonized enemy world effect.");
        }


        selectedUnit.MarkAttacked();
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

    void ResetUnitsForActiveOwnerIfNeeded()
    {
        string activeOwner = GetActiveOwner();
        if (string.IsNullOrEmpty(activeOwner) || activeOwner == lastActiveOwner)
        {
            return;
        }

        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in allUnits)
        {
            if (unit != null && unit.owner == activeOwner)
            {
                unit.ResetTurnActions();
            }
        }

        lastActiveOwner = activeOwner;
    }

    string GetActiveOwner()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null || gameManager.currentPlayer == null)
        {
            return "player";
        }

        if (ReferenceEquals(gameManager.currentPlayer, gameManager.player2))
        {
            return "enemy";
        }

        return "player";
    }

    bool IsEnemyTarget(HexTile tile)
    {
        // Ali: special units can target enemy world effects for colonization, while normal units keep classic unit/fort targeting.
        bool canTargetEnemyWorldEffect = selectedUnit != null
            && selectedUnit.canColonizeEnemyWorldEffects
            && tile != null
            && tile.tileType == "worldEffect";

        return tile != null
            && tile.owner != "none"
            && tile.owner != GetActiveOwner()
            && (tile.tileType == "unit" || tile.tileType == "fort" || canTargetEnemyWorldEffect);
    }

    IEnumerator MoveUnitSmoothly(Unit unit, Vector3 startPosition, Vector3 targetPosition)
    {
        isAnimatingUnit = true;

        Vector3 originalScale = unit != null ? unit.transform.localScale : Vector3.one;
        Quaternion originalRotation = unit != null ? unit.transform.rotation : Quaternion.identity;

        if (smoothMoveDuration <= 0f)
        {
            if (unit != null)
            {
                unit.transform.position = targetPosition;
                unit.transform.localScale = originalScale;
                unit.transform.rotation = originalRotation;
            }

            isAnimatingUnit = false;
            yield break;
        }

        float moveDirection = Mathf.Sign(targetPosition.x - startPosition.x);
        if (Mathf.Approximately(moveDirection, 0f))
        {
            moveDirection = 1f;
        }

        float elapsed = 0f;
        while (unit != null && elapsed < smoothMoveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / smoothMoveDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            float walkCycle = Mathf.Sin(progress * Mathf.PI * 4f);
            float bobOffset = Mathf.Abs(walkCycle) * walkBobHeight;
            float leanOffset = walkCycle * walkLeanAngle * -moveDirection;
            float squash = Mathf.Abs(walkCycle) * walkSquashAmount;

            unit.transform.position = Vector3.Lerp(startPosition, targetPosition, easedProgress) + Vector3.up * bobOffset;
            unit.transform.rotation = originalRotation * Quaternion.Euler(0f, 0f, leanOffset);
            unit.transform.localScale = new Vector3(
                originalScale.x * (1f + squash * 0.5f),
                originalScale.y * (1f - squash),
                originalScale.z
            );
            yield return null;
        }

        if (unit != null)
        {
            unit.transform.position = targetPosition;
            unit.transform.localScale = originalScale;
            unit.transform.rotation = originalRotation;
        }

        isAnimatingUnit = false;
    }
}
