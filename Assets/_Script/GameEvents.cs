using System;

public static class GameEvents
{
    public static bool BossDefeatedFlag { get; private set; }
    public static event Action BossDefeated;

    public static void RaiseBossDefeated()
    {
        if (BossDefeatedFlag) return;
        BossDefeatedFlag = true;
        BossDefeated?.Invoke();
    }

    public static void ResetStageFlags()
    {
        BossDefeatedFlag = false;
    }
}