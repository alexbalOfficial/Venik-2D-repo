using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    private AudioSource audioSource;

    [SerializeField] private float fadeSpeed = 1.5f;
    [SerializeField] private float musicVolume = 0.3f;

    private Coroutine fadeCoroutine;

    // Запоминаем позицию каждого трека
    private Dictionary<AudioClip, float> musicTimes =
        new Dictionary<AudioClip, float>();

    private AudioClip currentClip;

    private bool musicPausedByRadio;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.volume = musicVolume;
    }

    public void PlayMusic(AudioClip newClip)
    {
        if (newClip == null)
            return;

        // Если уже играет этот трек — ничего не делаем
        if (currentClip == newClip)
            return;

        // Сохраняем время прошлого трека
        if (currentClip != null)
        {
            musicTimes[currentClip] = audioSource.time;
        }

        currentClip = newClip;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToMusic(newClip));
    }

    private IEnumerator FadeToMusic(AudioClip newClip)
    {
        // Fade out
        while (audioSource.volume > 0)
        {
            audioSource.volume -= fadeSpeed * Time.deltaTime;
            yield return null;
        }

        audioSource.Stop();

        audioSource.clip = newClip;

        // Восстанавливаем позицию трека
        if (musicTimes.ContainsKey(newClip))
        {
            audioSource.time = musicTimes[newClip];
        }
        else
        {
            audioSource.time = 0;
        }

        audioSource.Play();

        // Fade in
        while (audioSource.volume < musicVolume)
        {
            audioSource.volume += fadeSpeed * Time.deltaTime;
            yield return null;
        }

        audioSource.volume = musicVolume;
    }

    // -------------------
    // RADIO CONTROL
    // -------------------

    public void PauseMusicForRadio()
    {
        if (musicPausedByRadio)
            return;

        musicPausedByRadio = true;

        audioSource.Pause();
    }

    public void ResumeMusicAfterRadio()
    {
        if (!musicPausedByRadio)
            return;

        musicPausedByRadio = false;

        audioSource.UnPause();
    }
}