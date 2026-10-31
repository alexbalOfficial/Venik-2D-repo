
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

public class PlayerSave : MonoBehaviour
{
    private CinemachineConfiner confiner;

    private void Start()
    {
        confiner = FindObjectOfType<CinemachineConfiner>();

        LoadGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    public void SaveGame()
    {
        int slot = SaveSystem.CurrentSlot;

        if (slot < 0 || slot >= SaveSystem.SlotCount)
        {
            Debug.LogWarning("Сохранение пропущено: слот не выбран.");
            return;
        }

        SaveData data = new SaveData
        {
            playerX = transform.position.x,
            playerY = transform.position.y,
            playerZ = transform.position.z,
            sceneName = SceneManager.GetActiveScene().name
        };

        if (confiner != null && confiner.m_BoundingShape2D != null)
        {
            data.cameraBounds =
                confiner.m_BoundingShape2D.gameObject.name;
        }

        SaveSystem.Save(slot, data);
        Debug.Log("Игра сохранена в слоте " + (slot + 1));
    }

    private void LoadGame()
    {
        int slot = SaveSystem.CurrentSlot;

        if (slot < 0 || slot >= SaveSystem.SlotCount)
            return;

        SaveData data = SaveSystem.Load(slot);

        if (data == null)
        {
            Debug.Log("В слоте " + (slot + 1) + " нет сохранения.");
            return;
        }

        transform.position = new Vector3(
            data.playerX,
            data.playerY,
            data.playerZ
        );

        if (string.IsNullOrEmpty(data.cameraBounds) || confiner == null)
            return;

        PolygonCollider2D[] colliders =
            FindObjectsOfType<PolygonCollider2D>(true);

        foreach (PolygonCollider2D col in colliders)
        {
            if (col.gameObject.name == data.cameraBounds)
            {
                confiner.m_BoundingShape2D = col;
                break;
            }
        }
    }
}
