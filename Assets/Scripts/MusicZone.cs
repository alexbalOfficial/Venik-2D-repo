using UnityEngine;

public class MusicZone : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        MusicManager.Instance.PlayMusic(musicClip);
    }
}