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

                Color sideColor = col < midCol
                    ? new Color(0.6f, 0.8f, 1f)
                    : new Color(1f, 0.7f, 0.7f);

                tile.SetBaseColor(sideColor);
            }
        }
    }

    void PlaceForts()
    {
        int midRow = gridHeight / 2;
        tiles[OffsetToAxial(0, midRow)].SetAsFort(new Color(0.1f, 0.2f, 0.8f), "player");
        tiles[OffsetToAxial(gridWidth - 1, midRow)].SetAsFort(new Color(0.8f, 0.1f, 0.1f), "enemy");
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
}
