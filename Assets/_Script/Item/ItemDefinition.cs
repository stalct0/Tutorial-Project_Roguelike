using UnityEngine;
using System;
using System.Collections.Generic;

public enum ItemKind { Passive, OnPickup}
public enum StatType { MaxHealth, CurrentHealth, CurrentAttackDamage, CurrentMoney, CurrentMoveSpeed, CurrentDashCoolDown}

[Serializable]
public struct StatDelta
{
    public StatType stat;
    public float amount;         // +10, -5 같은 가감치
    public bool isMultiplier;    // true면 배율(예: 0.1 = +10%)
}

[CreateAssetMenu(menuName = "Game/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;
    public ItemKind kind;

    [Header("공통 효과(패시브/즉시발동)")]
    public List<StatDelta> deltas = new();

    [Header("지속시간(선택)")]
    public bool hasDuration;
    public float durationSec = 0f;   // >0이면 일정 시간 후 원복
}
