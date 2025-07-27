using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;
    
public class MapGenerator : MonoBehaviour
{
    public Tilemap mainTilemap;
    public RoomPrefabLibrary roomLibrary;
    public GameObject playerPrefab;

    private MapConfig config;
    private RoomNode[,] roomGrid;
    private Grid gridLayout;

    void Awake()
    {
        gridLayout = mainTilemap.GetComponentInParent<Grid>();
    }
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

        Vector3 cellSize = gridLayout.cellSize;
        
        var roomResult = RoomTilePainter.PaintRooms(mainTilemap, roomGrid, roomLibrary, gridLayout.cellSize);
        if (roomResult.HasValue)
        {
            var (startRoomPrefab, startOffset) = roomResult.Value;

            Transform spawnPoint = startRoomPrefab.transform.Find("SpawnPoint");
            if (spawnPoint != null)
            {
                Vector3 spawnPos = spawnPoint.position + startOffset;
                GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

                var vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
                if (vcam != null)
                {
                    vcam.Follow = player.transform;
                }
            }
            
            GameObject.Destroy(startRoomPrefab); 
        }
        
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