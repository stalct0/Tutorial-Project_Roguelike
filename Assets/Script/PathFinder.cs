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
        Debug.Log($"start pos: {current.x},{current.y}");
        int i = 0;
        
        while (current.y < height - 1) // 매줄 재생
        {
            i++;
            bool isAtLeftEdge = current.x == 0;
            bool isAtRightEdge = current.x == width - 1;
            int dir = isAtLeftEdge ? 1 : isAtRightEdge ? -1 : (Random.value < 0.5f ? -1 : 1);
            current = new Vector2Int(current.x + dir, current.y); //무조건 한번 가로 이동
            path.Add(current);
            Debug.Log($"{i}: fchecked {isAtLeftEdge} {isAtRightEdge}");
            Debug.Log($"{i}: fmove {dir}");
            
            isAtLeftEdge = current.x == 0;
            isAtRightEdge = current.x == width - 1;
            
            while(!isAtLeftEdge && !isAtRightEdge)
            {
                isAtLeftEdge = current.x == 0;
                isAtRightEdge = current.x == width - 1;
                float rv = Random.value;
                Debug.Log($"{i}: {rv}");
                if (rv > 0.85f)
                {
                    break;
                }
                int newX = current.x + dir;
                if (newX >= 0 && newX < width)
                {
                    current = new Vector2Int(newX, current.y); //옆으로 이동
                    path.Add(current);
                    Debug.Log($"{i}: move {dir}");
                }
                else
                {
                    Debug.Log("error");
                }
                
                Debug.Log($"{i}:checked {isAtLeftEdge} {isAtRightEdge}");
            }
            current = new Vector2Int(current.x, current.y + 1); //위로 이동
            path.Add(current);
            Debug.Log($"{i}: Move up");
        }

        // 맨 윗줄 도달 → 오른쪽 or 왼쪽 방향 결정
        int lastDirection = Random.value < 0.5f ? 1 : -1;
        List<Vector2Int> finalRow = new();
        Vector2Int lastRowStart = current;
        
        while (current.x >= 0 && current.x < width)
        {
            current = new Vector2Int(current.x + lastDirection, current.y);
            if (!path.Contains(current))
            {
                finalRow.Add(current);
            }
        }
        
        if (finalRow.Count == 0) //버그걸려서 없다면
        {
            finalRow.Add(current); // 최소한 current는 넣어준다
            Debug.Log("error");
        }
        // end는 마지막 줄 중 하나를 랜덤으로 선택
        Vector2Int end = finalRow[Random.Range(0, finalRow.Count)];
        
        current = lastRowStart;
        
        while (current.x >= 0 && current.x < width)
        {
            if (!path.Contains(current))
            {
                if (current.x >= 0 && current.x < width && current.y >= 0 && current.y < height)
                {
                    path.Add(current);
                    Debug.Log($"last move {lastDirection}");
                }
            }

            if (current == end)
                break;
            
            int nextX = current.x + lastDirection;
            // 배열 범위 초과 방지
            if (nextX < 0 || nextX >= width)
            {
                Debug.Log("error");
                break;
            }
            current = new Vector2Int(nextX, current.y);
            
        }
        
        return new PathResult { path = path, end = end };

    }
}

