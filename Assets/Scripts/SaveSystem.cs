
using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public float playerX;
    public float playerY;
    public float playerZ;

    public string cameraBounds;
    public string sceneName;
    public string savedAt;
}

public static class SaveSystem
{
    public const int SlotCount = 8;

    public static int CurrentSlot { get; private set; } = -1;

    private static string GetPath(int slot)
    {
        return Path.Combine(
            Application.persistentDataPath,
            "save_" + (slot + 1) + ".json"
        );
    }

    public static bool HasSave(int slot)
    {
        return slot >= 0 &&
               slot < SlotCount &&
               File.Exists(GetPath(slot));
    }

    public static SaveData Load(int slot)
    {
        if (!HasSave(slot))
            return null;

        try
        {
            string json = File.ReadAllText(GetPath(slot));
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("Не удалось загрузить сохранение: " + e.Message);
            return null;
        }
    }

    public static void SetCurrentSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount)
        {
            Debug.LogError("Неверный номер слота: " + slot);
            return;
        }

        CurrentSlot = slot;
    }

    public static void Save(int slot, SaveData data)
    {
        if (slot < 0 || slot >= SlotCount || data == null)
        {
            Debug.LogError("Не удалось сохранить игру: неверный слот или данные.");
            return;
        }

        try
        {
            data.savedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetPath(slot), json);
        }
        catch (Exception e)
        {
            Debug.LogError("Не удалось сохранить игру: " + e.Message);
        }
    }
}
