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
        
        var result = PathFinder.FindPath(start, config.Width, config.Height);
        List<Vector2Int> path = result.path;
        Vector2Int end = result.end;
        RoomTypeAssigner.AssignTypes(roomGrid, path, start, end);
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


}