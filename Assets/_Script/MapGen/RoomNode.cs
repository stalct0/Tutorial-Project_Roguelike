using UnityEngine;
using System.Collections.Generic;

public class RoomNode // Room하나가 가지고 있는 
{
    public Vector2Int GridPosition; // 위치
    
    public RoomType Type = RoomType.None; //타입
    
    public bool IsInMainPath = false; //경로에 포함되는지 여부
}
