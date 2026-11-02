
using Cinemachine;
using UnityEngine;

public class MapTransition : MonoBehaviour
{
    [SerializeField] private PolygonCollider2D mapBoundry;
    [SerializeField] private Direction direction;
    [SerializeField] private Transform teleportTargetPosition;
    [SerializeField] private float addirivePos = 1f;

    private CinemachineConfiner confiner;

    private enum Direction
    {
        Up,
        Down,
        Left,
        Right,
        Teleport
    }

    // Общая блокировка для всех переходов между картами.
    private static bool transitionInProgress;

    private void Awake()
    {
        confiner = FindObjectOfType<CinemachineConfiner>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (transitionInProgress)
            return;

        GameObject target = null;

        if (collision.CompareTag("Player"))
        {
            target = collision.gameObject;
        }
        else
        {
            MotorcycleController motorcycle =
                collision.GetComponentInParent<MotorcycleController>();

            if (motorcycle != null && motorcycle.IsRiding)
                target = motorcycle.gameObject;
        }

        if (target != null)
            StartCoroutine(Transition(target));
    }

    private System.Collections.IEnumerator Transition(GameObject target)
    {
        transitionInProgress = true;

        try
        {
            PauseController.SetPause(true);

            // FadeOut/FadeIn у вас асинхронные, поэтому
            // используем отдельную последовательность ниже.
            var fadeOut = ScreenFader.Instance.FadeOut();
            while (!fadeOut.IsCompleted)
                yield return null;

            if (confiner != null && mapBoundry != null)
                confiner.m_BoundingShape2D = mapBoundry;

            if(direction == Direction.Teleport)
            {
                target.transform.position = teleportTargetPosition.position;

            }

            Vector3 newPos = target.transform.position;

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

            target.transform.position = newPos;

            var fadeIn = ScreenFader.Instance.FadeIn();
            while (!fadeIn.IsCompleted)
                yield return null;

            PauseController.SetPause(false);
        }
        finally
        {
            transitionInProgress = false;
        }
    }
}
