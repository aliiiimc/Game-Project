using UnityEngine;

public class Unit : MonoBehaviour
{
    public int moveRange = 2;
    public int attackRange = 1;
    public int health = 10;
    public int attack = 3;
    public string owner = "player";
    public HexTile currentTile;
    public bool hasMovedThisTurn;
    public bool hasAttackedThisTurn;

    // Ali: runtime flag copied from card data for world-effect capture rules.
    public bool canColonizeEnemyWorldEffects;


    // Ali: track whether a spawned unit is allowed to attack yet, instead of assuming every new unit is immediately ready.
    public bool isReadyToAttack = true;


    public bool CanMove()
    {
        return !hasMovedThisTurn && !hasAttackedThisTurn;
    }


    // Ali: a unit can only attack if it has not attacked yet and its summon/readiness rule allows it.
    public bool CanAttack()
    {
        return isReadyToAttack && !hasAttackedThisTurn;
    }


    public void MarkMoved()
    {
        hasMovedThisTurn = true;
    }

    public void MarkAttacked()
    {
        hasAttackedThisTurn = true;
    }

    public void ResetTurnActions()
    {
        hasMovedThisTurn = false;
        hasAttackedThisTurn = false;
    }

    public void PlaceOnTile(HexTile tile, bool snapToTile = true)
    {
        currentTile = tile;
        tile.tileType = "unit";
        tile.owner = owner;

        if (snapToTile)
        {
            transform.position = tile.transform.position;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (owner == "player")
            sr.color = Color.white; // blue
        else
            sr.color = new Color(1f, 0.3f, 0.3f);  // red
    }

    public void Die()
    {
        if (currentTile != null)
        {
            currentTile.RemoveUnit();
            currentTile = null;
        }
        Destroy(gameObject);
    }
}
