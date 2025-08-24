using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Loot Table", fileName = "LootTable")]
public class LootTable : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public ItemDefinition item;
        [Min(0)] public int weight = 1;          // 0이면 뽑히지 않음
    }

    public List<Entry> entries = new();

    /// <summary>가중치 랜덤으로 한 개 뽑기 (stage 기반 필터링 선택)</summary>
    public ItemDefinition Roll(int currentStage = 0, bool includeOnPickup = true)
    {
        // 후보 수집
        int total = 0;
        List<(ItemDefinition item, int weight)> pool = new();
        foreach (var e in entries)
        {
            if (e.item == null) continue;
            if (!includeOnPickup && e.item.kind == ItemKind.OnPickup) continue;
            if (e.weight <= 0) continue;

            pool.Add((e.item, e.weight));
            total += e.weight;
        }
        if (total <= 0 || pool.Count == 0) return null;

        int r = Random.Range(0, total);
        int acc = 0;
        foreach (var (item, weight) in pool)
        {
            acc += weight;
            if (r < acc) return item;
        }
        return pool[pool.Count - 1].item;
    }
}