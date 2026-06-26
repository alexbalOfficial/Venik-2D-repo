using UnityEngine;

public static class GameFlags
{
    public static bool Get(string flagName)
    {
        return PlayerPrefs.GetInt(flagName, 0) == 1;
    }

    public static void Set(string flagName, bool value)
    {
        PlayerPrefs.SetInt(flagName, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}