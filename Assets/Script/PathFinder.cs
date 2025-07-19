using System.Collections.Generic;
using UnityEngine;

public static class PathFinder // start에서 end로 가는 경로 생성
{
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, int width, int height)
    {
        List<Vector2Int> path = new(); //경로에 들어가는 모든 좌표를 저장
        Vector2Int current = start;
        path.Add(current);

        while (current.y < end.y)
        {
            bool moved = false; //한번이라도 움직였는지 확인

            if (Random.value < 0.8f) // 옆으로 먼저 움직이기
            {
                int dir = Random.value < 0.5f ? -1 : 1; // -1 또는 1 반환
                int newX = current.x + dir; 
                if (newX >= 0 && newX < width)
                {
                    current = new Vector2Int(newX, current.y);
                    path.Add(current);
                    moved = true;
                }
            }

            if (!moved || Random.value < 0.4f) // 위로 움직이기
            {
                current = new Vector2Int(current.x, current.y + 1);
                path.Add(current);
            }
        }

        return path; //경로 리스트 반환
    }
}
