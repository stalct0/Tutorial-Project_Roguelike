using System;
using UnityEngine;


[Serializable]
public class ItemInstance
{
    public ItemDefinition def;

    // 대표 스탯 (필요 시 확장 가능)
    public int valueRolled;
    public float durationRolled;

    // 생성 유틸
    public static ItemInstance FromDefinition(ItemDefinition d, int v, float dur)
    {
        return new ItemInstance { def = d, valueRolled = v, durationRolled = dur };
    }

    public string DisplayName => def != null ? def.displayName : "";
    public Sprite Icon => def != null ? def.icon : null;
    public string Description => def != null ? def.description : "";
    public ItemKind Kind => def != null ? def.kind : ItemKind.Passive;
}

