using UnityEngine;
using System.Collections.Generic;

public class HexGrid : MonoBehaviour
{
    public GameObject hexPrefab;
    public GameObject unitPrefab;
    public int gridWidth = 7;
    public int gridHeight = 5;
    public float hexSize = 0.5f;
    public bool spawnDebugUnits = true;
    public Color playerSideColor = new Color(0.16f, 0.42f, 0.47f);
    public Color enemySideColor = new Color(0.47f, 0.20f, 0.26f);
    public Color playerFortColor = new Color(0.12f, 0.70f, 0.78f);
    public Color enemyFortColor = new Color(0.82f, 0.20f, 0.26f);

    private Dictionary<AxialCoord, HexTile> tiles = new Dictionary<AxialCoord, HexTile>();

    void Start()
    {
        GenerateGrid();
        PlaceForts();

        if (spawnDebugUnits)
        {
            SpawnHorizontalDebugUnits();
        }
    }

    void GenerateGrid()
    {
        int midCol = gridWidth / 2;

        for (int row = 0; row < gridHeight; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                AxialCoord coord = OffsetToAxial(col, row);
                Vector3 position = AxialToWorld(coord);

                GameObject hex = Instantiate(hexPrefab, position, Quaternion.identity, transform);
                hex.name = $"Hex_{coord.q}_{coord.r}";

                HexTile tile = hex.GetComponent<HexTile>();
                tile.coord = coord;
                tiles[coord] = tile;

                Color sideColor = col < midCol ? playerSideColor : enemySideColor;

                tile.SetBaseColor(sideColor);
            }
        }
    }

    void PlaceForts()
    {
        int midRow = gridHeight / 2;
        tiles[OffsetToAxial(0, midRow)].SetAsFort(playerFortColor, "player");
        tiles[OffsetToAxial(gridWidth - 1, midRow)].SetAsFort(enemyFortColor, "enemy");
    }

    void SpawnHorizontalDebugUnits()
    {
        int midRow = gridHeight / 2;
        int playerCol = Mathf.Min(1, gridWidth - 1);
        int enemyCol = Mathf.Max(gridWidth - 2, 0);

        SpawnUnit(unitPrefab, GetTileByOffset(playerCol, midRow), "player");
        SpawnUnit(unitPrefab, GetTileByOffset(enemyCol, midRow), "enemy");
    }

    public Unit SpawnUnit(GameObject prefab, HexTile tile, string owner)
    {
        if (prefab == null || tile == null || !tile.IsEmpty())
        {
            return null;
        }

        GameObject unitObj = Instantiate(prefab, tile.transform.position, Quaternion.identity, transform);
        Unit unit = unitObj.GetComponent<Unit>();
        unit.owner = owner;
        unit.PlaceOnTile(tile);
        return unit;
    }

    public HexTile GetTile(AxialCoord coord)
    {
        tiles.TryGetValue(coord, out HexTile tile);
        return tile;
    }

    public HexTile GetTileByOffset(int col, int row)
    {
        return GetTile(OffsetToAxial(col, row));
    }

    // Converts visual grid column/row (odd-r offset) to axial coordinates.
    public static AxialCoord OffsetToAxial(int col, int row)
    {
        return new AxialCoord(col - (row - (row & 1)) / 2, row);
    }

    // Converts axial coordinate to world position (pointy-top hexes).
    public Vector3 AxialToWorld(AxialCoord coord)
    {
        float x = hexSize * (Mathf.Sqrt(3f) * coord.q + Mathf.Sqrt(3f) / 2f * coord.r);
        float y = hexSize * (1.5f * coord.r);
        return new Vector3(x, y, 0f);
    }




    //ALI : Khdemt 3la Spawn system + other functions used lih 



    public int AxialToOffsetColumn(AxialCoord coord) // Function to convert the tile from axial coordinates into a visual grid column 
    {
        return coord.q + (coord.r - (coord.r & 1)) / 2;  //coord.r & 1 verifie si ligne pair ou impair

    }


    public bool IsInPlayerDeploymentZone(AxialCoord coord, string playerKey)//checks whether a tile is inside the 2-column deployment area for a given side
    // cuz lcharacters hay spawniw f 2 lines lwlin dial lpartie dial lplayer fl board
    {
        int col = AxialToOffsetColumn(coord);

        if (playerKey == PlayerKeyResolver.PlayerOneKey) // si joueur 1, autorise juste lignes 1 et 2 
        {
            return col >= 0 && col < 2;
        }

        if (playerKey == PlayerKeyResolver.PlayerTwoKey) // si AI, autorise lignes 7 et 8
        {
            return col >= gridWidth - 2 && col < gridWidth;
        }

        return false;
    }

    public bool IsInPlayerHalf(AxialCoord coord, string playerKey) // Pour verifier si joueur dans sa moitié du board
    {
        int col = AxialToOffsetColumn(coord);
        int middle = gridWidth / 2;

        if (playerKey == PlayerKeyResolver.PlayerOneKey)
        {
            return col < middle;
        }

        if (playerKey == PlayerKeyResolver.PlayerTwoKey)
        {
            return col >= middle;
        }

        return false;
    }


    public Unit SpawnUnitFromCard(HexTile tile, string owner, CardRuntimeState card) //Spawn
    {
        if (tile == null || card == null || !tile.IsEmpty()) //Basic verification
        {
            return null;
        }

        Unit unit = SpawnUnit(unitPrefab, tile, owner);
        //SpawnUnit: instancie le prefab Unit, le place sur la case
        //met l’owner, met à jour la tile avec tileType = "unit"
        if (unit == null)
        {
            return null;
        }

        CharacterCardData characterCard = card.SourceCard as CharacterCardData; // essaie de convertir la carte en CharacterCardData
        if (characterCard == null) //Si la carte n’est pas une carte personnage.
        {
            return unit;
        }

        unit.sourceCharacterCardData = characterCard;

        SpriteRenderer spriteRenderer = unit.GetComponent<SpriteRenderer>(); //récupère le composant qui affiche l’image de l’unité sur le board.
        if (spriteRenderer != null && characterCard.manifestedSprite != null)
        {
            spriteRenderer.sprite = characterCard.manifestedSprite; //remplace le sprite du prefab par le sprite de la carte.
        }

        //Generate base stats when a card spawns
        unit.health = characterCard.maxHp;
        unit.attack = characterCard.attackDamage;
        unit.attackRange = Mathf.Max(0, characterCard.attackRange);

        // Ali: copy special capture capability from card definition to spawned unit.
        unit.canColonizeEnemyWorldEffects = characterCard.canColonizeEnemyWorldEffects;


        // Ali: copy summon attack readiness from the card definition so spawned units follow the intended v1 rule.
        unit.isReadyToAttack = characterCard.startsReadyToAttack;


        if (card.CurrentMovementCapacity.HasValue)
        {
            unit.moveRange = card.CurrentMovementCapacity.Value;
        }
        else if (characterCard.unitMovementCapacity.HasValue)
        {
            unit.moveRange = characterCard.unitMovementCapacity.Value;
        }

        // Ali: link the spawned board unit back to the card runtime so spells can target and update the real Unit.
        unit.LinkRuntimeCard(card);

        return unit;
    }
    //Ali end.
}




