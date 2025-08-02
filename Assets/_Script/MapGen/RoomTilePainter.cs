using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public static class RoomTilePainter
{
    public static (GameObject startRoomPrefab, Vector3 startOffsetTile, Vector3 startOffsetObj)?
        PaintRooms(Tilemap target, RoomNode[,] grid, RoomPrefabLibrary library, Vector3 cellSize)
    {
        // 방 크기 지정
        int roomWidth = 12; 
        int roomHeight = 10; 

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

                Vector3Int offsetTile = new Vector3Int(x * roomWidth, y * roomHeight, 0);
                Vector3 offsetObj = new Vector3(x * roomWidth * cellSize.x, y * roomHeight * cellSize.y, 0);
                CopyTiles(source, target, offsetTile); //타일맵에 타일 생성


                
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
}