using UnityEngine;

public class Radio : MonoBehaviour, IInteractable
{
    [Header("Audio")]
    public AudioClip[] songs; // список песен
    public AudioClip buttonClickSound; // звук кнопки

    private AudioSource audioSource;

    [Header("Visual")]
    public Sprite radioOffSprite;
    public Sprite radioOnSprite;

    [Header("Particles")]
    public ParticleSystem musicParticles; // частицы нот

    private SpriteRenderer sr;

    private bool isPlaying = false;
    private int currentSongIndex = 0;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        SetRadioState(false);

        // выключаем частицы при старте
        if (musicParticles != null)
            musicParticles.Stop();
    }

    void Update()
    {
        if (!isPlaying)
            return;

        if (audioSource.clip == null)
            return;

        // Если трек реально закончился, а не игра просто свернулась
        if (!audioSource.isPlaying && audioSource.time >= audioSource.clip.length - 0.1f)
        {
            PlayNextSong();
        }
    }

    public bool CanInteract() => true;

    public void Interact()
    {
        PlayButtonClick();

        if (isPlaying)
        {
            TurnOffRadio();
        }
        else
        {
            TurnOnRadio();
        }
    }

    private void TurnOnRadio()
    {
        if (songs.Length == 0) return;

        SetRadioState(true);

        MusicManager.Instance.PauseMusicForRadio();

        audioSource.clip = songs[currentSongIndex];
        audioSource.Play();

        if (musicParticles != null)
            musicParticles.Play();
    }

    private void TurnOffRadio()
    {
        audioSource.Stop();
        SetRadioState(false);

        if (musicParticles != null)
            musicParticles.Stop();

        MusicManager.Instance.ResumeMusicAfterRadio();

        currentSongIndex++;

        if (currentSongIndex >= songs.Length)
            currentSongIndex = 0;
    }

    private void PlayNextSong()
    {
        currentSongIndex++;

        if (currentSongIndex >= songs.Length)
            currentSongIndex = 0;

        audioSource.clip = songs[currentSongIndex];
        audioSource.Play();
    }

    private void PlayButtonClick()
    {
        if (buttonClickSound != null)
        {
            AudioSource.PlayClipAtPoint(
                buttonClickSound,
                transform.position,
                0.7f
            );
        }
    }

    private void SetRadioState(bool playing)
    {
        isPlaying = playing;
        sr.sprite = isPlaying ? radioOnSprite : radioOffSprite;
    }
}
