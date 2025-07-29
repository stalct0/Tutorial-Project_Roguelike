using System.Collections.Generic;
using UnityEngine;

public static class PathFinder // start에서 end로 가는 경로 생성
{
    public struct PathResult
    {
        public List<Vector2Int> path;
        public Vector2Int end;
    }

    public static PathResult FindPath(Vector2Int start, int width, int height)
    {
        List<Vector2Int> path = new();
        Vector2Int current = start;
        
        path.Add(current); //시작 위치
        int i = 0;
        
        while (current.y < height - 1) // 매줄 재생
        {
            i++;
            bool isAtLeftEdge = current.x == 0;
            bool isAtRightEdge = current.x == width - 1;
            int dir = isAtLeftEdge ? 1 : isAtRightEdge ? -1 : (Random.value < 0.5f ? -1 : 1);
            current = new Vector2Int(current.x + dir, current.y); //무조건 한번 가로 이동
            path.Add(current);
            
            isAtLeftEdge = current.x == 0;
            isAtRightEdge = current.x == width - 1;
            
            while(!isAtLeftEdge && !isAtRightEdge)
            {
                isAtLeftEdge = current.x == 0;
                isAtRightEdge = current.x == width - 1;
                float rv = Random.value;
                if (rv > 0.75f)
                {
                    break;
                }
                int newX = current.x + dir;
                if (newX >= 0 && newX < width)
                {
                    current = new Vector2Int(newX, current.y); //옆으로 이동
                    path.Add(current);
                }
                
            }
            current = new Vector2Int(current.x, current.y + 1); //위로 이동
            path.Add(current);
        }

        // 맨 윗줄 도달 → 오른쪽 or 왼쪽 방향 결정
        bool LastisAtLeftEdge = current.x == 0;
        bool LastisAtRightEdge = current.x == width - 1;
        int lastDirection = LastisAtLeftEdge ? 1 : LastisAtRightEdge ? -1 : (Random.value < 0.5f ? -1 : 1);
        List<Vector2Int> finalRow = new();
        Vector2Int lastRowStart = current;
        Debug.Log($"{lastDirection}");
        
        while (current.x >= 0 && current.x < width)
        {
            if (!path.Contains(current))
            {
                    finalRow.Add(current);
            }
            current = new Vector2Int(current.x + lastDirection, current.y);
        }
        
        if (finalRow.Count == 0) //버그걸려서 없다면
        {
            finalRow.Add(current); // 최소한 current는 넣어준다
        }
        Debug.Log($"final row: {finalRow.Count}");
        // end는 마지막 줄 중 하나를 랜덤으로 선택
        Vector2Int end = finalRow[Random.Range(0, finalRow.Count)];
        Debug.Log($"end pos: {end.x},{end.y}");
        
        current = lastRowStart;
        
        while (current.x >= 0 && current.x < width)
        {
            if (!path.Contains(current))
            {
                if (current.x >= 0 && current.x < width && current.y >= 0 && current.y < height)
                {
                    path.Add(current);
                }
            }

            if (current == end)
                break;
            
            int nextX = current.x + lastDirection;
            // 배열 범위 초과 방지
            if (nextX < 0 || nextX >= width)
            {
                break;
            }
            current = new Vector2Int(nextX, current.y);
            
        }
        
        return new PathResult { path = path, end = end };

    }
}

