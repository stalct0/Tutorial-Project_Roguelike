using System.Collections.Generic;
using UnityEngine;

public static class RoomTypeAssigner
{
    public static void AssignTypes(RoomNode[,] grid, List<Vector2Int> path)
    {
        foreach (Vector2Int pos in path)
        {
            grid[pos.x, pos.y].IsInMainPath = true; //경로에 들어가는 room들 다 지정
        }

        for (int i = 1; i < path.Count; i++) // 경로의 길에 들어가는 room개수 만큼 반복
        {
            Vector2Int from = path[i - 1];
            Vector2Int to = path[i];

            RoomNode node = grid[to.x, to.y];

            if (to.y > from.y)
                node.Type = RoomType.Climb;
            else if (to.x != from.x)
                node.Type = RoomType.Corridor;
        }

        // 나머지 Room
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var node = grid[x, y];
            if (!node.IsInMainPath)
                node.Type = RoomType.NonCritical;
        }
    }
}