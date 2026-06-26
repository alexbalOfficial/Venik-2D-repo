using UnityEngine;
using Cinemachine;

public class PlayerSave : MonoBehaviour
{
    private CinemachineConfiner confiner;

    private void Start()
    {
        confiner = FindObjectOfType<CinemachineConfiner>();

        LoadPosition();
        LoadCameraBounds();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    public void SaveGame()
    {
        // Позиция игрока
        PlayerPrefs.SetFloat("PlayerX", transform.position.x);
        PlayerPrefs.SetFloat("PlayerY", transform.position.y);

        // Текущий Bounding Shape
        if (confiner != null && confiner.m_BoundingShape2D != null)
        {
            PlayerPrefs.SetString(
                "CameraBounds",
                confiner.m_BoundingShape2D.gameObject.name
            );
        }

        PlayerPrefs.Save();
    }

    private void LoadPosition()
    {
        if (PlayerPrefs.HasKey("PlayerX"))
        {
            transform.position = new Vector3(
                PlayerPrefs.GetFloat("PlayerX"),
                PlayerPrefs.GetFloat("PlayerY"),
                transform.position.z
            );
        }
    }

    private void LoadCameraBounds()
    {
        if (!PlayerPrefs.HasKey("CameraBounds"))
            return;

        string boundsName = PlayerPrefs.GetString("CameraBounds");

        PolygonCollider2D[] colliders =
            FindObjectsOfType<PolygonCollider2D>(true);

        foreach (PolygonCollider2D col in colliders)
        {
            if (col.gameObject.name == boundsName)
            {
                confiner.m_BoundingShape2D = col;
                break;
            }
        }
    }
}