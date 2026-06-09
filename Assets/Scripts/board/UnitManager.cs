// Rabie: Added shared public unit action methods so computer AI can query and execute legal movement and attacks through the same board rules as the player.
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public float smoothMoveDuration = 0.25f;
    public float walkBobHeight = 0.08f;
    public float walkLeanAngle = 4f;
    public float walkSquashAmount = 0.08f;
    public Color moveHighlightColor = new Color(0.42f, 0.93f, 0.68f);
    public Color attackHighlightColor = new Color(1f, 0.70f, 0.30f);
    public Color attackRangeHighlightColor = new Color(1f, 0.35f, 0.35f, 0.45f);

    private Unit selectedUnit;
    private List<HexTile> moveTiles = new List<HexTile>();
    private List<HexTile> attackTiles = new List<HexTile>();
    private List<HexTile> attackRangeTiles = new List<HexTile>();
    private HexGrid grid;
    private WorldEffectManager worldEffectManager;
    private readonly List<ISpecialCardScript> specialCardScripts = new List<ISpecialCardScript>
    {
        new Archer(),
        new Bomber(),
        new Dragon(),
        new Engineer(),
        new EuropeanKing(),
        new Miner(),
        new Priest(),
        new UfoCow()
    };
    private readonly Hospital hospital = new Hospital();

    private GameManager gameManager; //Ali
    private string lastActiveOwner = "";
    private bool isAnimatingUnit;


    void Start()
    {
        EnsureReferences();
        ResetUnitsForActiveOwnerIfNeeded();
    }


    void Update()
    {
        ResetUnitsForActiveOwnerIfNeeded();

        if (isAnimatingUnit)
        {
            return;
        }

        if (IsComputerControlledTurn())
        {
            if (selectedUnit != null)
            {
                DeselectUnit();
            }

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
                // Attack a legal target in attack range (enemy by default, or special-card targets).
                if (attackTiles.Contains(clickedTile) && IsEnemyTarget(clickedTile))
                {
                    AttackTarget(clickedTile);
                }
                // Move to an empty tile in move range
                else if (moveTiles.Contains(clickedTile) && IsValidMoveDestination(clickedTile))
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

        // Attack range boundary — show all tiles within attack range in a distinct color first.
        // Movement and enemy target highlights will overwrite where they overlap.
        int effectiveAttackRange = GetAttackRangeForUnit(selectedUnit);
        if (effectiveAttackRange > 0)
        {
            EnsureReferences();
            if (grid != null)
            {
                List<HexTile> allAttackRangeTiles = HexUtils.GetTilesInRange(selectedUnit.currentTile, effectiveAttackRange, grid);
                foreach (HexTile t in allAttackRangeTiles)
                {
                    if (t != selectedUnit.currentTile)
                    {
                        t.Highlight(attackRangeHighlightColor);
                        attackRangeTiles.Add(t);
                    }
                }
            }
        }

        // Movement range (green). The range uses the unit's remaining turn budget, not the full card range again.
        moveTiles = GetLegalMoveTiles(selectedUnit);
        foreach (HexTile t in moveTiles)
        {
            t.Highlight(moveHighlightColor);
        }

        // Attack targets (orange) — highlight enemies within attackRange
        attackTiles = GetLegalAttackTargets(selectedUnit);
        foreach (HexTile t in attackTiles)
        {
            t.Highlight(attackHighlightColor);
        }
    }

    public List<Unit> GetUnitsForOwner(string owner)
    {
        List<Unit> ownedUnits = new List<Unit>();
        if (string.IsNullOrWhiteSpace(owner))
        {
            return ownedUnits;
        }

        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in allUnits)
        {
            if (unit != null && unit.owner == owner)
            {
                ownedUnits.Add(unit);
            }
        }

        return ownedUnits;
    }

    public List<HexTile> GetLegalMoveTiles(Unit unit)
    {
        List<HexTile> legalTiles = new List<HexTile>();
        if (unit == null || unit.currentTile == null || !unit.CanMove())
        {
            return legalTiles;
        }

        EnsureReferences();
        if (grid == null)
        {
            return legalTiles;
        }

        legalTiles = HexUtils.GetReachableMoveTiles(
            unit.currentTile,
            unit.GetRemainingMovement(),
            grid,
            GetMovementType(unit.sourceCharacterCardData));
        AppendReachableEnemyMineTiles(unit.currentTile, unit, legalTiles);
        legalTiles.RemoveAll(tile => !IsInsideTurnStartRange(unit, tile) || !IsValidMoveDestination(unit, tile));
        return legalTiles;
    }

    public void NotifyUnitSpawned(Unit unit, CharacterCardData unitCardData)
    {
        if (unit == null || unitCardData == null)
        {
            return;
        }

        ISpecialCardScript specialScript = ResolveSpecialScript(unit, out _);
        specialScript?.OnAfterSpawn(unit, unitCardData);
    }

    public List<HexTile> GetLegalAttackTargets(Unit unit)
    {
        List<HexTile> legalTargets = new List<HexTile>();
        if (unit == null || unit.currentTile == null || !unit.CanAttack())
        {
            return legalTargets;
        }

        EnsureReferences();
        if (grid == null)
        {
            return legalTargets;
        }

        int effectiveAttackRange = GetAttackRangeForUnit(unit);
        List<HexTile> tilesInRange = HexUtils.GetTilesInRange(unit.currentTile, effectiveAttackRange, grid);
        foreach (HexTile tile in tilesInRange)
        {
            if (IsEnemyTarget(unit, tile))
            {
                legalTargets.Add(tile);
            }
        }

        return legalTargets;
    }

    public bool TryMoveUnit(Unit unit, HexTile targetTile)
    {
        if (isAnimatingUnit || unit == null || targetTile == null)
        {
            return false;
        }

        List<HexTile> legalTiles = GetLegalMoveTiles(unit);
        if (!legalTiles.Contains(targetTile))
        {
            return false;
        }

        return ExecuteMoveUnit(unit, targetTile);
    }

    public bool TryAttackTarget(Unit attacker, HexTile targetTile)
    {
        if (isAnimatingUnit || attacker == null || targetTile == null)
        {
            return false;
        }

        List<HexTile> legalTargets = GetLegalAttackTargets(attacker);
        if (!legalTargets.Contains(targetTile))
        {
            return false;
        }

        return ExecuteAttackTarget(attacker, targetTile);
    }

    void MoveUnit(HexTile targetTile)
    {
        if (!TryMoveUnit(selectedUnit, targetTile))
        {
            DeselectUnit();
            return;
        }

        DeselectUnit();
    }

    bool ExecuteMoveUnit(Unit movingUnit, HexTile targetTile)
    {
        if (movingUnit == null || targetTile == null || !movingUnit.CanMove())
        {
            return false;
        }

        EnsureReferences();
        if (grid == null || movingUnit.currentTile == null)
        {
            return false;
        }

        CharacterCardData unitCardData;
        ISpecialCardScript specialScript = ResolveSpecialScript(movingUnit, out unitCardData);
        int movementCost = HexUtils.GetMoveDistance(
            movingUnit.currentTile,
            targetTile,
            grid,
            movingUnit.GetRemainingMovement(),
            GetMovementType(unitCardData));

        if (movementCost <= 0)
        {
            return false;
        }

        if (!IsInsideTurnStartRange(movingUnit, targetTile) || !IsValidMoveDestination(movingUnit, targetTile))
        {
            return false;
        }

        Vector3 startPosition = movingUnit.transform.position;
        Vector3 targetPosition = targetTile.transform.position;
        Mines mines = new Mines();
        bool steppedOnEnemyMine = mines.TryTriggerMine(movingUnit, targetTile, worldEffectManager, out int mineDamage);

        if (steppedOnEnemyMine)
        {
            if (worldEffectManager == null)
            {
                worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
            }
        }

        specialScript?.OnBeforeMove(movingUnit, unitCardData);
        movingUnit.currentTile.ClearUnitOccupant();
        movingUnit.PlaceOnTile(targetTile, snapToTile: false);
        bool consumeMoveAction = specialScript == null || specialScript.ConsumeMoveAction(movingUnit, unitCardData);
        if (consumeMoveAction)
        {
            movingUnit.MarkMoved(movementCost);
        }
        else
        {
            Debug.Log($"[SpecialTrigger][Miner] Move action preserved after moving to ({targetTile.coord.q},{targetTile.coord.r}).");
        }

        if (steppedOnEnemyMine)
        {
            movingUnit.health -= mineDamage;
            Debug.Log($"[SpecialTrigger][Mines] Mine triggered at ({targetTile.coord.q},{targetTile.coord.r}). {movingUnit.name} took {mineDamage} damage. HP now {movingUnit.health}.");
            if (movingUnit.health <= 0)
            {
                movingUnit.Die();
                return true;
            }
        }

        StartCoroutine(MoveUnitSmoothly(movingUnit, startPosition, targetPosition, specialScript, unitCardData, targetTile));
        return true;
    }

    void AttackTarget(HexTile targetTile)
    {
        if (!TryAttackTarget(selectedUnit, targetTile))
        {
            DeselectUnit();
            return;
        }

        DeselectUnit();
    }

    bool ExecuteAttackTarget(Unit attacker, HexTile targetTile)
    {
        if (attacker == null || targetTile == null || !attacker.CanAttack())
        {
            return false;
        }

        EnsureReferences();
        string activeOwner = GetOwnerForUnit(attacker);
        CharacterCardData attackerCardData;
        ISpecialCardScript specialScript = ResolveSpecialScript(attacker, out attackerCardData);
        bool isSpecialTarget = specialScript != null && specialScript.CanTarget(attacker, attackerCardData, targetTile, activeOwner);
        if (isSpecialTarget && specialScript.TryHandleAttack(attacker, attackerCardData, targetTile, activeOwner))
        {
            attacker.MarkAttacked();
            return true;
        }

        Unit target = FindUnitOnTile(targetTile);
        if (target != null)
        {
            ProjectileVisualSettings projectileVisuals = SpecialCardScriptBase.GetProjectileVisualSettings(attackerCardData);
            if (projectileVisuals != null && projectileVisuals.projectilePrefab != null)
            {
                ProjectileVisual.Spawn(
                    projectileVisuals,
                    attacker.transform.position,
                    targetTile.transform.position);
            }

            target.ApplyDamage(attacker.attack);
            Debug.Log($"Attacked! Target health: {target.health}");

            if (target.health <= 0)
            {
                Debug.Log("Target died!");
            }
        }
        else if (targetTile.tileType == "fort") //Ali : Update de AttackTarget
        {
            Debug.Log("Attacked fort!");

            ProjectileVisualSettings projectileVisuals = SpecialCardScriptBase.GetProjectileVisualSettings(attackerCardData);
            if (projectileVisuals != null && projectileVisuals.projectilePrefab != null)
            {
                ProjectileVisual.Spawn(
                    projectileVisuals,
                    attacker.transform.position,
                    targetTile.transform.position);
            }

            if (gameManager == null)
            {
                Debug.LogWarning("GameManager not found. Cannot damage fort.");
                return false;
            }

            if (targetTile.owner == "enemy")
            {
                gameManager.DamagePlayer2Fort(attacker.attack);
            }
            else if (targetTile.owner == "player")
            {
                gameManager.DamagePlayer1Fort(attacker.attack);
            }
            else
            {
                return false;
            }
        }
        // Ali: colonization rule - special units can convert enemy world effects instead of dealing damage.
        else if (targetTile.HasWorldEffect()
                 && attacker.canColonizeEnemyWorldEffects
                 && targetTile.worldEffectOwner != activeOwner)
        {
            if (worldEffectManager == null)
            {
                worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
            }

            if (worldEffectManager != null && worldEffectManager.TryColonize(targetTile, activeOwner))
            {
                Debug.Log("Colonized enemy world effect.");
            }
            else
            {
                Debug.LogWarning("Colonization failed: world effect manager rejected this target.");
                return false;
            }
        }
        else if (targetTile.HasWorldEffect()
                 && targetTile.worldEffectOwner != "none"
                 && targetTile.worldEffectOwner != activeOwner)
        {
            ProjectileVisualSettings projectileVisuals = SpecialCardScriptBase.GetProjectileVisualSettings(attackerCardData);
            if (projectileVisuals != null && projectileVisuals.projectilePrefab != null)
            {
                ProjectileVisual.Spawn(
                    projectileVisuals,
                    attacker.transform.position,
                    targetTile.transform.position);
            }

            if (worldEffectManager == null)
            {
                worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
            }

            if (worldEffectManager == null || !worldEffectManager.TryDamageWorldEffect(targetTile, attacker.attack, out int dealtDamage))
            {
                Debug.LogWarning("World effect attack failed.");
                return false;
            }

            Debug.Log($"Attacked world effect for {dealtDamage} damage.");
        }
        else if (isSpecialTarget)
        {
            Debug.LogWarning("Special target was selected but the special card did not handle this attack.");
            return false;
        }
        else
        {
            return false;
        }


        attacker.MarkAttacked();
        return true;
    }

    void DeselectUnit()
    {
        foreach (HexTile t in attackRangeTiles) t.ResetColor();
        foreach (HexTile t in moveTiles) t.ResetColor();
        foreach (HexTile t in attackTiles) t.ResetColor();
        attackRangeTiles.Clear();
        moveTiles.Clear();
        attackTiles.Clear();
        selectedUnit = null;
    }

    public Unit FindUnitOnTile(HexTile tile)
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

        ResetUnitsForOwner(activeOwner);
        lastActiveOwner = activeOwner;
    }

    public void ResetUnitsForCurrentOwnerTurn(bool force = false)
    {
        string activeOwner = GetActiveOwner();
        if (string.IsNullOrEmpty(activeOwner))
        {
            return;
        }

        if (!force && activeOwner == lastActiveOwner)
        {
            return;
        }

        ResetUnitsForOwner(activeOwner);
        lastActiveOwner = activeOwner;
    }

    void ResetUnitsForOwner(string activeOwner)
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in allUnits)
        {
            if (unit != null && unit.owner == activeOwner)
            {
                unit.ResetTurnActions();
                CharacterCardData unitCardData;
                ISpecialCardScript specialScript = ResolveSpecialScript(unit, out unitCardData);
                specialScript?.OnOwnerTurnStart(unit, unitCardData);
            }
        }
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

    bool IsComputerControlledTurn()
    {
        EnsureReferences();
        return gameManager != null
            && gameManager.computerPlayer != null
            && gameManager.currentPlayer != null
            && ReferenceEquals(gameManager.currentPlayer, gameManager.player2);
    }

    bool IsEnemyTarget(HexTile tile)
    {
        return IsEnemyTarget(selectedUnit, tile);
    }

    bool IsEnemyTarget(Unit unit, HexTile tile)
    {
        if (tile == null || unit == null)
        {
            return false;
        }

        string activeOwner = GetOwnerForUnit(unit);
        CharacterCardData attackerCardData;
        ISpecialCardScript specialScript = ResolveSpecialScript(unit, out attackerCardData);
        bool canSpecialTarget = specialScript != null
            && specialScript.CanTarget(unit, attackerCardData, tile, activeOwner);
        GetUnitAttackProfile(unit, attackerCardData, out AttackType attackType, out global::AttackTarget attackTarget);

        if (tile.owner == "none" && tile.worldEffectOwner == "none")
        {
            return false;
        }

        // Special scripts may intentionally target allied tiles (for example Engineer repairing allied structures).
        if (canSpecialTarget)
        {
            return true;
        }

        if (attackType == AttackType.HealFix)
        {
            return false;
        }

        string targetOwner = tile.HasWorldEffect() && tile.tileType != "unit" && tile.tileType != "fort"
            ? tile.worldEffectOwner
            : tile.owner;

        // Ali: special units can target enemy world effects for colonization, while normal units keep classic unit/fort targeting.
        bool canTargetEnemyWorldEffect = unit.canColonizeEnemyWorldEffects
            && tile.HasWorldEffect();

        if (targetOwner == activeOwner)
        {
            return false;
        }

        if (tile.tileType == "unit")
        {
            Unit targetUnit = FindUnitOnTile(tile);
            bool targetIsAir = IsAirUnit(targetUnit);
            if (!CanProfileTarget(attackTarget, targetIsAir))
            {
                return false;
            }

            return true;
        }

        if (tile.tileType == "fort")
        {
            return CanProfileTarget(attackTarget, false);
        }

        if (tile.tileType == "worldEffect" || tile.HasWorldEffect())
        {
            return CanProfileTarget(attackTarget, false) || canTargetEnemyWorldEffect;
        }

        return false;
    }

    bool IsValidMoveDestination(HexTile tile)
    {
        return IsValidMoveDestination(selectedUnit, tile);
    }

    bool IsValidMoveDestination(Unit unit, HexTile tile)
    {
        if (unit == null || tile == null)
        {
            return false;
        }

        return tile.CanUnitOccupy() || IsEnemyMineTileForUnit(tile, unit);
    }

    bool IsEnemyMineTileForUnit(HexTile tile, Unit unit)
    {
        Mines mines = new Mines();
        return mines.IsEnemyMineTileForUnit(tile, unit);
    }

    void AppendReachableEnemyMineTiles(HexTile startTile, Unit unit, List<HexTile> destinationTiles)
    {
        if (startTile == null || unit == null || destinationTiles == null || grid == null)
        {
            return;
        }

        List<HexTile> inRangeTiles = HexUtils.GetTilesInRange(startTile, unit.GetRemainingMovement(), grid);
        for (int i = 0; i < inRangeTiles.Count; i++)
        {
            HexTile tile = inRangeTiles[i];
            if (!IsEnemyMineTileForUnit(tile, unit))
            {
                continue;
            }

            MovementType movementType = GetMovementType(unit.sourceCharacterCardData);
            int distance = HexUtils.GetMoveDistance(startTile, tile, grid, unit.GetRemainingMovement(), movementType);
            if (distance <= 0)
            {
                continue;
            }

            if (!destinationTiles.Contains(tile))
            {
                destinationTiles.Add(tile);
            }
        }
    }

    bool IsInsideTurnStartRange(Unit unit, HexTile tile)
    {
        if (unit == null || tile == null || unit.turnStartTile == null)
        {
            return true;
        }

        return HexUtils.GetHexDistance(unit.turnStartTile, tile) <= unit.GetEffectiveMoveRange();
    }

    int GetAttackRangeForUnit(Unit unit)
    {
        if (unit == null)
        {
            return 0;
        }

        CharacterCardData unitCardData;
        ISpecialCardScript specialScript = ResolveSpecialScript(unit, out unitCardData);
        if (specialScript != null)
        {
            return Mathf.Max(0, specialScript.GetAttackRange(unit, unitCardData));
        }

        return Mathf.Max(0, unit.attackRange);
    }

    string GetOwnerForUnit(Unit unit)
    {
        if (unit != null && !string.IsNullOrWhiteSpace(unit.owner))
        {
            return unit.owner;
        }

        return GetActiveOwner();
    }

    void GetUnitAttackProfile(Unit unit, CharacterCardData cardData, out AttackType attackType, out global::AttackTarget attackTarget)
    {
        attackType = AttackType.Melee;
        attackTarget = global::AttackTarget.Ground;

        if (cardData == null)
        {
            return;
        }

        attackType = cardData.attackType;
        attackTarget = cardData.attackTarget;

        ISpecialCardScript specialScript = ResolveSpecialScript(unit, out _);
        if (specialScript != null)
        {
            attackType = specialScript.GetAttackType(unit, cardData);
        }
    }

    static bool CanProfileTarget(global::AttackTarget attackTarget, bool targetIsAir)
    {
        if (attackTarget == global::AttackTarget.Both)
        {
            return true;
        }

        if (targetIsAir)
        {
            return attackTarget == global::AttackTarget.Air;
        }

        return attackTarget == global::AttackTarget.Ground;
    }

    static bool IsAirUnit(Unit unit)
    {
        if (unit == null || unit.sourceCharacterCardData == null)
        {
            return false;
        }

        return GetMovementType(unit.sourceCharacterCardData) == MovementType.Flying;
    }

    static MovementType GetMovementType(CharacterCardData cardData)
    {
        if (cardData == null)
        {
            return MovementType.Ground;
        }

        return cardData.movementType;
    }

    void EnsureReferences()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>(); //Def de GameManager
        }

        if (grid == null)
        {
            grid = FindFirstObjectByType<HexGrid>();
        }

        if (worldEffectManager == null)
        {
            worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
        }
    }

    ISpecialCardScript ResolveSpecialScript(Unit unit, out CharacterCardData unitCardData)
    {
        unitCardData = unit != null ? unit.sourceCharacterCardData : null;
        if (unit == null || unitCardData == null)
        {
            return null;
        }

        for (int i = 0; i < specialCardScripts.Count; i++)
        {
            ISpecialCardScript script = specialCardScripts[i];
            if (script != null && script.IsMatch(unit, unitCardData))
            {
                return script;
            }
        }

        return null;
    }

    IEnumerator MoveUnitSmoothly(Unit unit, Vector3 startPosition, Vector3 targetPosition, ISpecialCardScript specialScript, CharacterCardData unitCardData, HexTile destinationTile)
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

            specialScript?.OnAfterMove(unit, unitCardData, destinationTile);
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

        specialScript?.OnAfterMove(unit, unitCardData, destinationTile);
        isAnimatingUnit = false;
    }
}
