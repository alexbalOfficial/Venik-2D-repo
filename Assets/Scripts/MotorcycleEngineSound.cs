
using UnityEngine;
using UnityEngine.InputSystem;
public class MotorcycleEngineSound : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MotorcycleController motorcycle;

    [Header("Engine clips")]
    [SerializeField] private AudioClip yupakStart;
    [SerializeField] private AudioClip yupakIdle;
    [SerializeField] private AudioClip yupakRun;

    [Header("Settings")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
    [SerializeField] private float crossfadeSpeed = 8f;
    [SerializeField] private float movingThreshold = 0.2f;

    private AudioSource startSource;
    private AudioSource idleSource;
    private AudioSource runSource;

    private bool wasRiding;
    private bool startupPhase;

    private void Awake()
    {
        if (motorcycle == null)
            motorcycle = GetComponent<MotorcycleController>();

        startSource = CreateSource("Engine Start");
        idleSource = CreateSource("Engine Idle");
        runSource = CreateSource("Engine Run");
    }

    private AudioSource CreateSource(string sourceName)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
        source.volume = 0f;

        return source;
    }
    private void Start()
    {
        PrepareClip(yupakStart);
        PrepareClip(yupakIdle);
        PrepareClip(yupakRun);
    }

    private void PrepareClip(AudioClip clip)
    {
        if (clip != null && !clip.preloadAudioData)
            clip.LoadAudioData();
    }

    private void Update()
    {
        if (motorcycle == null)
            return;

        bool riding = motorcycle.IsRiding;

        if (riding && !wasRiding)
            BeginStartup();

        if (!riding && wasRiding)
            StopAllSounds();

        if (riding && startupPhase)
        {
            if (!startSource.isPlaying || !motorcycle.IsEngineStarting)
            {
                startSource.Stop();
                startupPhase = false;

                if (yupakIdle != null)
                {
                    idleSource.clip = yupakIdle;
                    idleSource.loop = true;
                    idleSource.volume = volume;
                    idleSource.Play();
                }
            }
        }

        if (riding && !startupPhase)
        {
            bool moving = false;

            bool movingInput = false;

            if (Keyboard.current != null)
            {
                movingInput =
                    Keyboard.current.wKey.isPressed ||
                    Keyboard.current.aKey.isPressed ||
                    Keyboard.current.sKey.isPressed ||
                    Keyboard.current.dKey.isPressed;
            }

            bool acceleratingOrCruising =
                movingInput && motorcycle.SpeedChange >= -0.5f;

            UpdateLoop(idleSource, yupakIdle, !acceleratingOrCruising);
            UpdateLoop(runSource, yupakRun, acceleratingOrCruising);
        }
        else
        {
            UpdateLoop(idleSource, yupakIdle, false);
            UpdateLoop(runSource, yupakRun, false);
        }

        wasRiding = riding;
    }

    private void BeginStartup()
    {
        StopAllSounds();

        startupPhase = true;

        if (yupakStart != null)
        {
            startSource.clip = yupakStart;
            startSource.loop = false;
            startSource.volume = volume;
            startSource.Play();
        }
        else
        {
            Debug.LogWarning(
                "MotorcycleEngineSound: yupak_start is not assigned.",
                this
            );

            startupPhase = false;
        }
    }

    private void UpdateLoop(
        AudioSource source,
        AudioClip clip,
        bool shouldPlay)
    {
        if (clip == null)
        {
            source.volume = 0f;
            source.Stop();
            return;
        }

        if (shouldPlay)
        {
            if (source.clip != clip)
            {
                source.Stop();
                source.clip = clip;
                source.loop = true;
                source.volume = 0f;
            }

            if (!source.isPlaying)
                source.Play();
        }

        float targetVolume = shouldPlay ? volume : 0f;

        source.volume = Mathf.MoveTowards(
            source.volume,
            targetVolume,
            crossfadeSpeed * Time.deltaTime
        );

        if (!shouldPlay && source.volume <= 0.001f)
            source.Stop();
    }

    private void StopAllSounds()
    {
        startupPhase = false;

        if (startSource != null)
            startSource.Stop();

        if (idleSource != null)
            idleSource.Stop();

        if (runSource != null)
            runSource.Stop();
    }

    private void OnDisable()
    {
        StopAllSounds();
        wasRiding = false;
    }
}
