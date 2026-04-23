using UnityEngine;
using System.Collections.Generic;

public class HexGrid : MonoBehaviour
{
    public GameObject hexPrefab;
    public GameObject unitPrefab;
    public int gridWidth = 7;
    public int gridHeight = 5;
    public float hexSize = 0.5f;

    private Dictionary<AxialCoord, HexTile> tiles = new Dictionary<AxialCoord, HexTile>();

    void Start()
    {
        GenerateGrid();
        PlaceForts();
        SpawnTestUnit();
    }

    void GenerateGrid()
    {
        int midRow = gridHeight / 2;

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

                SpriteRenderer sr = hex.GetComponent<SpriteRenderer>();
                if (row < midRow)
                    sr.color = new Color(0.6f, 0.8f, 1f);
                else
                    sr.color = new Color(1f, 0.7f, 0.7f);
            }
        }
    }

    void PlaceForts()
    {
        int midCol = gridWidth / 2;
        tiles[OffsetToAxial(midCol, 0)].SetAsFort(new Color(0.1f, 0.2f, 0.8f), "player");
        tiles[OffsetToAxial(midCol, gridHeight - 1)].SetAsFort(new Color(0.8f, 0.1f, 0.1f), "enemy");
    }

    void SpawnTestUnit()
    {
        HexTile tile = tiles[OffsetToAxial(3, 2)];
        GameObject unitObj = Instantiate(unitPrefab, tile.transform.position, Quaternion.identity);
        Unit unit = unitObj.GetComponent<Unit>();
        unit.owner = "player";
        unit.PlaceOnTile(tile);
    }

    public HexTile GetTile(AxialCoord coord)
    {
        tiles.TryGetValue(coord, out HexTile tile);
        return tile;
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
