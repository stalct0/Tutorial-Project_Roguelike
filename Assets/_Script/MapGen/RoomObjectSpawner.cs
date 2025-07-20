using UnityEngine;

public static class RoomObjectSpawner
{
    public static void CopyRoomObjects(GameObject roomPrefab, Vector3 worldOffset)
    {
        foreach (Transform child in roomPrefab.transform)
        {
            // Tilemap은 무시
            if (child.GetComponent<UnityEngine.Tilemaps.Tilemap>() != null)
                continue;

            GameObject copy = Object.Instantiate(child.gameObject);
            copy.transform.position = child.position + worldOffset;
        }
    }
}