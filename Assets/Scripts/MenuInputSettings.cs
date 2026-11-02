
using UnityEngine;
using UnityEngine.InputSystem;

public enum MenuAction
{
    Up,
    Down,
    Left,
    Right,
    Confirm,
    Back
}

public static class MenuInputSettings
{
    private static Key GetDefaultKey(MenuAction action)
    {
        switch (action)
        {
            case MenuAction.Up: return Key.W;
            case MenuAction.Down: return Key.S;
            case MenuAction.Left: return Key.A;
            case MenuAction.Right: return Key.D;
            case MenuAction.Confirm: return Key.E;
            case MenuAction.Back: return Key.Escape;
            default: return Key.None;
        }
    }

    private static string GetPreferenceKey(MenuAction action)
    {
        return "MenuKey_" + action;
    }

    public static Key GetKey(MenuAction action)
    {
        string preferenceKey = GetPreferenceKey(action);

        if (!PlayerPrefs.HasKey(preferenceKey))
            return GetDefaultKey(action);

        Key key = (Key)PlayerPrefs.GetInt(preferenceKey);

        if (!System.Enum.IsDefined(typeof(Key), key))
            return GetDefaultKey(action);

        return key;
    }

    public static void SetKey(MenuAction action, Key key)
    {
        PlayerPrefs.SetInt(GetPreferenceKey(action), (int)key);
        PlayerPrefs.Save();
    }

    public static void ResetToDefaults()
    {
        foreach (MenuAction action in System.Enum.GetValues(typeof(MenuAction)))
        {
            PlayerPrefs.DeleteKey(GetPreferenceKey(action));
        }

        PlayerPrefs.Save();
    }

    public static bool WasPressed(MenuAction action)
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return false;

        Key key = GetKey(action);

        if (key != Key.None && keyboard[key].wasPressedThisFrame)
            return true;

        // Enter остаётся дополнительной клавишей подтверждения.
        if (action == MenuAction.Confirm)
        {
            return keyboard.enterKey.wasPressedThisFrame ||
                   keyboard.numpadEnterKey.wasPressedThisFrame;
        }

        // Escape всегда остаётся запасной клавишей возврата.
        if (action == MenuAction.Back)
            return keyboard.escapeKey.wasPressedThisFrame;

        return false;
    }
}
