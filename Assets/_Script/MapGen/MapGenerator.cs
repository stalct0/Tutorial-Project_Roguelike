using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;
    
public class MapGenerator : MonoBehaviour
{
    [Header("World Trigger Bounds (tiles padding)")]
    public int padLeft  = 3;
    public int padRight = 3;
    public int padDown  = 3;
    public int padUp    = 3;
    
    [Header("World Trigger Bounds")]
    public string boundsObjectName = "MapTriggerBounds";
    public int boundsLayer = 0;             
    
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
        
        CreateOrUpdateWorldTriggerBounds(config.Width, config.Height, roomWidth, roomHeight, gridLayout.cellSize);

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

    void CreateOrUpdateWorldTriggerBounds(int roomsX, int roomsY, int roomW, int roomH, Vector3 cellSize)
    {
        // 맵의 "타일 단위" 크기
        int mapTilesW = roomsX * roomW;
        int mapTilesH = roomsY * roomH;

        // 패딩(타일)을 월드 단위로 변환
        float extraW = (padLeft + padRight) * cellSize.x;
        float extraH = (padDown + padUp)   * cellSize.y;

        // 맵 자체의 월드 크기
        float worldW = mapTilesW * cellSize.x;
        float worldH = mapTilesH * cellSize.y;

        // 콜라이더의 최종 사이즈(월드 단위)
        Vector2 size = new Vector2(worldW + extraW, worldH + extraH);

        // 콜라이더의 월드 중심
        //  - 기본 맵은 (0,0)부터 오른쪽/위로 깔리므로, 중심은 맵 절반 + (패딩의 좌우/상하 차이) 절반
        float centerX = (mapTilesW * 0.5f + (padRight - padLeft) * 0.5f) * cellSize.x;
        float centerY = (mapTilesH * 0.5f + (padUp    - padDown) * 0.5f) * cellSize.y;
        Vector3 center = new Vector3(centerX, centerY, 0f);

        // 기존 오브젝트가 있으면 재사용, 없으면 생성
        GameObject go = GameObject.Find(boundsObjectName);
        if (go == null)
            go = new GameObject(boundsObjectName);

        go.layer = boundsLayer;
        go.transform.position = center;

        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;      // ✅ 요청: 트리거 ON
        box.size = size;
        
    }

}