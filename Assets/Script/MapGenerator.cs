using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public Tilemap mainTilemap;
    public RoomPrefabLibrary roomLibrary;

    private MapConfig config;
    private RoomNode[,] roomGrid;

    void Start()
    {
        config = new MapConfig();
        config.GenerateRandomSize();

        roomGrid = new RoomNode[config.Width, config.Height];

        InitRooms();
        Vector2Int start = PickStartRoom();
        Vector2Int end = PickEndRoom();

        List<Vector2Int> path = PathFinder.FindPath(start, end, config.Width, config.Height);
        RoomTypeAssigner.AssignTypes(roomGrid, path);
        RoomTilePainter.PaintRooms(mainTilemap, roomGrid, roomLibrary);
    }

    void InitRooms()
    {
        for (int x = 0; x < config.Width; x++)
        for (int y = 0; y < config.Height; y++)
            roomGrid[x, y] = new RoomNode { GridPosition = new Vector2Int(x, y) };
    }

    Vector2Int PickStartRoom()
    {
        int x = Random.Range(0, config.Width);
        return new Vector2Int(x, 0); // bottom row
    }

    Vector2Int PickEndRoom()
    {
        int x = Random.Range(0, config.Width);
        return new Vector2Int(x, config.Height - 1); // top row
    }
}