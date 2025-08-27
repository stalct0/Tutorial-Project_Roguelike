using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BossMapGenerator : MonoBehaviour
{
    [Header("Tilemaps & Prefabs")]
    public Tilemap mainTilemap;
    public Tilemap ladderTilemap;
    public Tilemap borderTilemap;
    public Tilemap backgroundTilemap;
    public TileBase wallTile;
    public RoomPrefabLibrary roomLibrary;

    [Header("Layout Settings")]
    public int roomWidth = 12;
    public int roomHeight = 10;
    public int borderThickness = 10;

    [Header("Room Types")]
    [Tooltip("십자가(중앙+상하좌우)에 사용할 타입")]
    public RoomType singleRoomType = RoomType.NonCritical;
    [Tooltip("모서리 4개 방에 사용할 타입")]
    public RoomType cornerRoomType = RoomType.Corridor;

    [Header("World Trigger Bounds (optional)")]
    public string boundsObjectName = "MapTriggerBounds";
    public int boundsLayer = 0;
    public int padLeft = 3, padRight = 3, padDown = 3, padUp = 3;

    private RoomNode[,] roomGrid;
    private Grid gridLayout;

    void Awake() => gridLayout = mainTilemap.GetComponentInParent<Grid>();

    void Start() => GenerateBossMap();

    void GenerateBossMap()
    {
        // 3x3 고정
        int width = 3, height = 3;
        roomGrid = new RoomNode[width, height];
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            roomGrid[x, y] = new RoomNode { GridPosition = new Vector2Int(x, y), Type = RoomType.None };

        // 십자가(중앙+상하좌우) → singleRoomType
        var cross = new List<Vector2Int> {
            new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(2,1)
        };
        foreach (var p in cross) roomGrid[p.x, p.y].Type = singleRoomType;

        // 모서리 4방 → cornerRoomType
        var corners = new List<Vector2Int> {
            new Vector2Int(0,0), new Vector2Int(0,2),
            new Vector2Int(2,0), new Vector2Int(2,2)
        };
        foreach (var p in corners) roomGrid[p.x, p.y].Type = cornerRoomType;

        var targetTilemaps = new Dictionary<string, Tilemap> {
            { "MainTilemap", mainTilemap },
            { "LadderTilemap", ladderTilemap },
            { "BackGroundTilemap", backgroundTilemap }
        };

        // 방 페인팅 (RoomPrefabLibrary가 타입별 프리팹을 반환)
        RoomTilePainter.PaintRooms(
            targetTilemaps, roomGrid, roomLibrary,
            gridLayout.cellSize, roomWidth, roomHeight
        ); // :contentReference[oaicite:0]{index=0}

        // 플레이어 스폰: 중앙 방 중심
        Vector3 cellSize = gridLayout.cellSize;
        Vector3 spawnPos = Vector3.Scale(
            new Vector3(1 * roomWidth + roomWidth * 0.5f, 1 * roomHeight + roomHeight * 0.5f, 0f),
            cellSize
        );
        var ladderTM = ladderTilemap != null ? ladderTilemap : GameObject.Find("LadderTilemap")?.GetComponent<Tilemap>();
        GameManager.Instance.SpawnOrMovePlayer(spawnPos, ladderTM);

        // 외곽 벽
        RoomTilePainter.PaintBorder(borderTilemap, wallTile, width * roomWidth, height * roomHeight, borderThickness);

        CreateOrUpdateWorldTriggerBounds(width, height, roomWidth, roomHeight, cellSize);
    }

    void CreateOrUpdateWorldTriggerBounds(int roomsX, int roomsY, int roomW, int roomH, Vector3 cellSize)
    {
        int mapTilesW = roomsX * roomW;
        int mapTilesH = roomsY * roomH;

        float extraW = (padLeft + padRight) * cellSize.x;
        float extraH = (padDown + padUp)   * cellSize.y;

        float worldW = mapTilesW * cellSize.x;
        float worldH = mapTilesH * cellSize.y;

        Vector2 size = new Vector2(worldW + extraW, worldH + extraH);

        float centerX = (mapTilesW * 0.5f + (padRight - padLeft) * 0.5f) * cellSize.x;
        float centerY = (mapTilesH * 0.5f + (padUp    - padDown) * 0.5f) * cellSize.y;
        Vector3 center = new Vector3(centerX, centerY, 0f);

        GameObject go = GameObject.Find(boundsObjectName) ?? new GameObject(boundsObjectName);
        go.layer = boundsLayer;
        go.transform.position = center;

        var box = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;
    }
}