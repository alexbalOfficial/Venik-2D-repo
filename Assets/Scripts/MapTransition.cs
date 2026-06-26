using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTransition : MonoBehaviour
{
    [SerializeField] PolygonCollider2D mapBoundry;
    CinemachineConfiner confiner;
    [SerializeField] Direction direction;
    [SerializeField] float addirivePos = 1f;

    enum Direction { Up, Down, Left, Right }
    private void Awake()
    {
        confiner = FindObjectOfType<CinemachineConfiner>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            FadeTransition(collision.gameObject);
        }
    }

    async void FadeTransition(GameObject player)
    {
        PauseController.SetPause(true);
        await ScreenFader.Instance.FadeOut();
        confiner.m_BoundingShape2D = mapBoundry;
        UpdatePlayerPosition(player);
        await ScreenFader.Instance.FadeIn();
        PauseController.SetPause(false);
    }

    private void UpdatePlayerPosition(GameObject player)
    {
        Vector3 newPos = player.transform.position;

        switch (direction)
        {
            case Direction.Up:
                newPos.y += addirivePos;
                break;
            case Direction.Down:
                newPos.y -= addirivePos;
                break;
                case Direction.Left:
                newPos.x += addirivePos;
                break;
            case Direction.Right:
                newPos.x -= addirivePos;
                break;
        }

        player.transform.position = newPos;
    }
}
