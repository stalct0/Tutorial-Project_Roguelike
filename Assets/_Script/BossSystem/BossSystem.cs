using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;

public class BossSystem : MonoBehaviour
{
    Transform spawnPos;

    private void Awake()
    {
        Tilemap ladderTilemap = GameObject.Find("LadderTilemap").GetComponent<Tilemap>();
        GameManager.Instance.SpawnOrMovePlayer(Vector3.zero, ladderTilemap);
    }
}
