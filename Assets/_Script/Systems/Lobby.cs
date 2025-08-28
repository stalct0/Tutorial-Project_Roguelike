using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;
public class Lobby : MonoBehaviour
{
    void Awake()
    {
        Tilemap ladderTilemap = GameObject.Find("LadderTilemap").GetComponent<Tilemap>();
        Vector3 startpos = new Vector3(0, -2, 0);
        GameManager.Instance.SpawnOrMovePlayer(startpos, ladderTilemap);
    }
   
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
