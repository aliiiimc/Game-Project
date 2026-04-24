using UnityEngine;

public class Unit : MonoBehaviour
{
    public int moveRange = 2;
    public int attackRange = 1;
    public int health = 10;
    public int attack = 3;
    public string owner = "player";
    public HexTile currentTile;

    public void PlaceOnTile(HexTile tile)
{
    currentTile = tile;
    tile.tileType = "unit";
    tile.owner = owner;
    transform.position = tile.transform.position;

    SpriteRenderer sr = GetComponent<SpriteRenderer>();
    if (owner == "player")
        sr.color = new Color(0.2f, 0.4f, 1f);  // blue
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