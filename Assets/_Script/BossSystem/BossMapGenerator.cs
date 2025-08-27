using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BossMapGenerator : MonoBehaviour
{
    [Header("Tilemaps & Prefabs")] public Tilemap mainTilemap;
    public Tilemap ladderTilemap;
    public Tilemap borderTilemap;
    public Tilemap backgroundTilemap;
    public TileBase wallTile;
    public RoomPrefabLibrary roomLibrary;

    [Header("Layout Settings")] public int roomWidth = 12;
    public int roomHeight = 10;
    public int borderThickness = 10;

    [Header("Room Types")] [Tooltip("십자가(중앙+상하좌우)에 사용할 타입")]
    public RoomType crossRoomType = RoomType.NonCritical;
    [Tooltip("모서리 4개 방에 사용할 타입")] public RoomType cornerRoomType = RoomType.Corridor;
    [Tooltip("가운데 1방에 사용할 타입")] public RoomType centerRoomType = RoomType.Top;

    [Header("World Trigger Bounds (optional)")]
    public string boundsObjectName = "MapTriggerBounds";

    public int boundsLayer = 0;
    public int padLeft = 3, padRight = 3, padDown = 3, padUp = 3;

    private RoomNode[,] roomGrid;
    private Grid gridLayout;

    void Awake() => gridLayout = mainTilemap.GetComponentInParent<Grid>();

    void Start()
    {
        GameEvents.ResetStageFlags();
        GenerateBossMap();
    }
    
     void GenerateBossMap()
    {
        // 3x3 고정 그리드
        int width = 3, height = 3;
        roomGrid = new RoomNode[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            roomGrid[x, y] = new RoomNode { GridPosition = new Vector2Int(x, y), Type = RoomType.None };

        // ── 배치 ─────────────────────────────────────────
        // 중앙(1,1): centerRoomType
        roomGrid[1, 1].Type = centerRoomType;

        // 십자가 4방: crossRoomType
        roomGrid[1, 2].Type = crossRoomType; // Up
        roomGrid[1, 0].Type = crossRoomType; // Down
        roomGrid[0, 1].Type = crossRoomType; // Left
        roomGrid[2, 1].Type = crossRoomType; // Right

        // 모서리 4방: cornerRoomType
        roomGrid[0, 0].Type = cornerRoomType; // DL
        roomGrid[0, 2].Type = cornerRoomType; // UL
        roomGrid[2, 0].Type = cornerRoomType; // DR
        roomGrid[2, 2].Type = cornerRoomType; // UR
        // ────────────────────────────────────────────────

        // 소스 프리팹의 자식 타일맵 이름과 일치해야 복사됨
        var targetTilemaps = new Dictionary<string, Tilemap>
        {
            { "MainTilemap", mainTilemap },
            { "LadderTilemap", ladderTilemap },
            { "BackGroundTilemap", backgroundTilemap }
        };

        // 방 그리기(타입별 프리팹을 복사하여 월드에 배치)
        RoomTilePainter.PaintRooms(
            targetTilemaps, roomGrid, roomLibrary,
            gridLayout.cellSize, roomWidth, roomHeight
        ); // 타일/오브젝트 복사 로직 내부 참조. :contentReference[oaicite:3]{index=3}

        // 플레이어 스폰: 중앙 방의 중앙
        Vector3 cellSize = gridLayout.cellSize;
        Vector3 spawnPos = Vector3.Scale(
            new Vector3(1 * roomWidth + roomWidth * 0.5f, 1 * roomHeight + roomHeight * 0.5f, 0f),
            cellSize
        );
        var ladderTM = ladderTilemap != null ? ladderTilemap : GameObject.Find("LadderTilemap")?.GetComponent<Tilemap>();
        GameManager.Instance.SpawnOrMovePlayer(spawnPos, ladderTM);

        // 외곽 벽
        RoomTilePainter.PaintBorder(borderTilemap, wallTile, width * roomWidth, height * roomHeight, borderThickness);

        // 월드 트리거 바운즈(선택)
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