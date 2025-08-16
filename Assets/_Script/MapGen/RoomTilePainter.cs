    using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public static class RoomTilePainter
{
    public static (GameObject startRoomPrefab, Vector3 startOffsetTile, Vector3 startOffsetObj)?
        PaintRooms(Dictionary<string, Tilemap> targetTilemaps, RoomNode[,] grid, RoomPrefabLibrary library, Vector3 cellSize, int roomWidth, int roomHeight)
    {
        // 맵 전체 크기
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        GameObject startRoomPrefab = null;
        Vector3 startOffsetTile = Vector3.zero;
        Vector3 startOffsetObj = Vector3.zero;
        
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++) //모든 좌표에 반복
            {
                RoomNode node = grid[x, y];
                GameObject prefab = library.GetRandomPrefab(node.Type);
                if (prefab == null) continue;

                GameObject temp = GameObject.Instantiate(prefab); 
                Tilemap source = temp.GetComponentInChildren<Tilemap>();

                var tilemapsInRoom = temp.GetComponentsInChildren<Tilemap>();
                Vector3Int offsetTile = new Vector3Int(x * roomWidth, y * roomHeight, 0);
                Vector3 offsetObj = new Vector3(x * roomWidth * cellSize.x, y * roomHeight * cellSize.y, 0);
                foreach (var tm in tilemapsInRoom)
                {
                    if (targetTilemaps.TryGetValue(tm.gameObject.name, out var targetTilemap))
                    {
                        CopyTiles(tm, targetTilemap, offsetTile); //타일맵에 타일 생성
                    }
                    
                }
                
                CopyObjects(temp.transform, offsetObj, cellSize);
                
                if (node.Type == RoomType.Start) //start는 남겨두기
                {
                    startRoomPrefab = temp;
                    startOffsetTile = offsetTile;
                    startOffsetObj = offsetObj;
                }
                else
                {
                    GameObject.Destroy(temp);
                }
                
            }
        
        if (startRoomPrefab != null)
            return (startRoomPrefab, startOffsetTile, startOffsetObj); //MapGenerator한테 prefab이랑 offset 넘겨주기
        else
            return null;
    }

    static void CopyTiles(Tilemap from, Tilemap to, Vector3Int offset) // 타일맵에 타일 생성
    {
        BoundsInt bounds = from.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            TileBase tile = from.GetTile(pos);
            if (tile != null)
            {
                to.SetTile(pos + offset, tile);
            }
        }
    }
    static void CopyObjects(Transform parent, Vector3 offset, Vector3 cellSize)
    {
        foreach (Transform child in parent)
        {
            if (child.GetComponent<Tilemap>() != null)
                continue;

            GameObject clone = Object.Instantiate(child.gameObject);
            clone.transform.position = offset + Vector3.Scale(child.localPosition, cellSize); 
            
            clone.transform.rotation = child.localRotation;          
        }
    }
    
    public static void PaintBorder(Tilemap borderTilemap, TileBase wallTile, int mapWidth, int mapHeight, int thickness)
    {
        // 아래쪽 벽
        for (int x = -thickness; x < mapWidth + thickness; x++)
        for (int y = -thickness; y < 0; y++)
            borderTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);

        // 위쪽 벽
        for (int x = -thickness; x < mapWidth + thickness; x++)
        for (int y = mapHeight; y < mapHeight + thickness; y++)
            borderTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);

        // 왼쪽 벽
        for (int x = -thickness; x < 0; x++)
        for (int y = 0; y < mapHeight; y++)
            borderTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);

        // 오른쪽 벽
        for (int x = mapWidth; x < mapWidth + thickness; x++)
        for (int y = 0; y < mapHeight; y++)
            borderTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
    }
    
}
    
