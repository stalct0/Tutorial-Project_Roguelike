using System.Collections.Generic;
using UnityEngine;

public static class RoomTypeAssigner
{
    public static void AssignTypes(RoomNode[,] grid, List<Vector2Int> path, Vector2Int start, Vector2Int end)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        
        // 메인 경로 표시
        foreach (Vector2Int pos in path)
        {
            grid[pos.x, pos.y].IsInMainPath = true;
        }
        


        // 나머지 경로 따라 Climb/Corridor 결정
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2Int from = path[i - 1];
            Vector2Int current = path[i];
            Vector2Int to = path[i + 1];

            RoomNode node = grid[current.x, current.y];

            if (to.y > current.y)
                node.Type = RoomType.Climb;
            else if (current.y > from.y)
                node.Type = RoomType.Top;
            else if (current.x != from.x)
                node.Type = RoomType.Corridor;
        }
        
        // Start 지정
        var startNode = grid[start.x, start.y];
        startNode.Type = RoomType.Start;
        
        // End 지정
        var endNode = grid[end.x, end.y];
        endNode.Type = RoomType.End;
        
        // 나머지 방은 NonCritical
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var node = grid[x, y];
            if (!node.IsInMainPath)
                node.Type = RoomType.NonCritical;
        }
    }
}