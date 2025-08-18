using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;
public class Lobby : MonoBehaviour
{
    void Awake()
    {
        Tilemap ladderTilemap = GameObject.Find("LadderTilemap").GetComponent<Tilemap>();
        GameManager.Instance.SpawnOrMovePlayer(Vector3.zero, ladderTilemap);
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
