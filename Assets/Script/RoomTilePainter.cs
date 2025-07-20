using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public static class RoomTilePainter
{
    public static void PaintRooms(Tilemap target, RoomNode[,] grid, RoomPrefabLibrary library)
    {
        // 방 크기 지정
        int roomWidth = 12; 
        int roomHeight = 10; 

        // 맵 전체 크기
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++) //모든 좌표에 반복
            {
                RoomNode node = grid[x, y];
                GameObject prefab = library.GetRandomPrefab(node.Type);
                if (prefab == null) continue;

                GameObject temp = GameObject.Instantiate(prefab); 
                Tilemap source = temp.GetComponentInChildren<Tilemap>();

                Vector3Int offset = new Vector3Int(x * roomWidth, y * roomHeight, 0);
                Copy(source, target, offset); //타일맵에 타일 생성

                GameObject.Destroy(temp);
            }
    }

    static void Copy(Tilemap from, Tilemap to, Vector3Int offset) // 타일맵에 타일 생성
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
}