using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class RoomPrefabEntry
{
    public RoomType type;
    public List<GameObject> prefabs;
}

public class RoomPrefabLibrary : MonoBehaviour
{
    public List<RoomPrefabEntry> entries;

    public GameObject GetRandomPrefab(RoomType type)
    {
        var entry = entries.FirstOrDefault(e => e.type == type);
        if (entry == null || entry.prefabs.Count == 0) return null;

        return entry.prefabs[Random.Range(0, entry.prefabs.Count)];
    }
}