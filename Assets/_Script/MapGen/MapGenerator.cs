using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;
    
public class MapGenerator : MonoBehaviour
{
    public Tilemap mainTilemap;
    public Tilemap ladderTilemap;
    public Tilemap borderTilemap;
    public TileBase wallTile;
    
    public RoomPrefabLibrary roomLibrary;
    public GameObject playerPrefab;

    private MapConfig config;
    private RoomNode[,] roomGrid;
    private Grid gridLayout;

    private int roomWidth = 12;
    private int roomHeight = 10;
    private int thickness = 10;
    
    void Awake()
    {
        gridLayout = mainTilemap.GetComponentInParent<Grid>();
    }
    void Start()
    {
        config = new MapConfig();
        config.GenerateRandomSize(); // 맵 크기 랜덤

        var targetTilemaps = new Dictionary<string, Tilemap>
        {
            { "MainTilemap", mainTilemap },
            { "LadderTilemap", ladderTilemap }
        };
        
        roomGrid = new RoomNode[config.Width, config.Height];
    
        InitRooms();
        Vector2Int start = PickStartRoom();
        
        var result = PathFinder.FindPath(start, config.Width, config.Height);
        List<Vector2Int> path = result.path;
        Vector2Int end = result.end;
        RoomTypeAssigner.AssignTypes(roomGrid, path, start, end);

        Vector3 cellSize = gridLayout.cellSize;
        
        var roomResult = RoomTilePainter.PaintRooms(targetTilemaps, roomGrid, roomLibrary, gridLayout.cellSize, roomWidth, roomHeight);
        if (roomResult.HasValue)
        {
            var (startRoomPrefab, startOffset, st) = roomResult.Value;

            Transform spawnPoint = startRoomPrefab.transform.Find("SpawnPoint");
            if (spawnPoint != null)
            {
                Vector3 spawnPos = st + Vector3.Scale(spawnPoint.position, cellSize);
                var ladderTilemap = GameObject.Find("LadderTilemap").GetComponent<Tilemap>();
                
                GameManager.Instance.SpawnOrMovePlayer(spawnPos, ladderTilemap);
            }
            
            GameObject.Destroy(startRoomPrefab); 
        }
        
        RoomTilePainter.PaintBorder(borderTilemap, wallTile, config.Width * roomWidth, 
            config.Height * roomHeight, thickness);
        
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