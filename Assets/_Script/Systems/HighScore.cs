using UnityEngine;

public static class HighScore
{
    const string KEY_STAGE = "HS.Stage";
    const string KEY_LEVEL = "HS.Level";

    public static (int stage, int level) Get()
    {
        int s = PlayerPrefs.GetInt(KEY_STAGE, 1);
        int l = PlayerPrefs.GetInt(KEY_LEVEL, 1);
        return (s, l);
    }

    public static bool IsBetter(int stage, int level)
    {
        var (bestS, bestL) = Get();
        return (stage > bestS) || (stage == bestS && level > bestL);
    }

    public static bool TrySet(int stage, int level)
    {
        if (!IsBetter(stage, level)) return false;
        PlayerPrefs.SetInt(KEY_STAGE, stage);
        PlayerPrefs.SetInt(KEY_LEVEL, level);
        PlayerPrefs.Save();
        return true;
    }

    public static string GetAsText()  // "x-x" 형식으로 표시용
    {
        var (s, l) = Get();
        return $"{s}-{l}";
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KEY_STAGE);
        PlayerPrefs.DeleteKey(KEY_LEVEL);
    }
}