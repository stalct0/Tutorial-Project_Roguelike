using UnityEngine;
using System.Collections.Generic;

public class MapConfig
{
    public int Width { get; private set; } // 내부에서만 변경가능하게
    public int Height { get; private set; }

    public void GenerateRandomSize() 
    {
        Width = Random.Range(4, 6);   // 4 또는 5
        Height = Random.Range(4, 6);  // 4 또는 5
    }
}
