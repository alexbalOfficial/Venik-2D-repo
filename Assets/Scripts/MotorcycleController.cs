
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class MotorcycleController : MonoBehaviour, IInteractable
{
    [Header("Collider sizes")]
    [SerializeField] private Vector2 horizontalColliderSize = new Vector2(1.2f, 0.55f);
    [SerializeField] private Vector2 verticalColliderSize = new Vector2(0.55f, 1.2f);
    [SerializeField] private Vector2 horizontalColliderOffset = Vector2.zero;
    [SerializeField] private Vector2 verticalColliderOffset = Vector2.zero;

    private BoxCollider2D motorcycleCollider;

    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform exitPoint;

    [Header("Motorcycle sprites - without player")]
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;

    [Header("Motorcycle sprites - with player")]
    [SerializeField] private Sprite upWithPlayer;
    [SerializeField] private Sprite downWithPlayer;
    [SerializeField] private Sprite leftWithPlayer;
    [SerializeField] private Sprite rightWithPlayer;

    [Header("Initial direction")]
    [SerializeField] private Direction initialDirection = Direction.Down;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 7f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float reverseAcceleration = 8f;

    [Header("Dismount")]
    [SerializeField] private Vector2 defaultExitOffset = new Vector2(1.2f, 0f);

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private Vector2 heading;
    private Vector2 currentVelocity;
    private Vector2 inputDirection;

    public bool IsRiding { get; private set; }
    [Header("Engine startup")]
    [SerializeField] private float engineStartDuration = 2f;

    public bool IsEngineStarting { get; private set; }

    private float engineStartTimer;

    public float CurrentSpeed => currentVelocity.magnitude;
    public float SpeedChange { get; private set; }
    private bool justMounted;

    private Transform rider;
    private Rigidbody2D riderRb;
    private RigidbodyType2D originalRiderBodyType;
    private float riderZ;

    private PlayerMovement riderMovement;
    private Collider2D[] riderColliders;
    private bool[] originalColliderStates;

    private SpriteRenderer[] riderRenderers;
    private bool[] originalRendererStates;



    private const string MotorcycleXKey = "MotorcycleX";
    private const string MotorcycleYKey = "MotorcycleY";
    private const string MotorcycleHeadingXKey = "MotorcycleHeadingX";
    private const string MotorcycleHeadingYKey = "MotorcycleHeadingY";
    private const string MotorcycleRidingKey = "MotorcycleWasRiding";

    private void Start()
    {
        LoadSavedStateImmediately();
    }

    private void LoadSavedStateImmediately()
    {
        if (PlayerPrefs.HasKey(MotorcycleXKey))
        {
            Vector3 savedPosition = new Vector3(
                PlayerPrefs.GetFloat(MotorcycleXKey),
                PlayerPrefs.GetFloat(MotorcycleYKey),
                transform.position.z
            );

            transform.position = savedPosition;
            rb.position = new Vector2(savedPosition.x, savedPosition.y);
        }

        if (PlayerPrefs.HasKey(MotorcycleHeadingXKey))
        {
            heading = new Vector2(
                PlayerPrefs.GetFloat(MotorcycleHeadingXKey),
                PlayerPrefs.GetFloat(MotorcycleHeadingYKey)
            );

            if (heading.sqrMagnitude > 0.001f)
                heading.Normalize();
            else
                heading = DirectionToVector(initialDirection);
        }

        UpdateSprite();

        if (PlayerPrefs.GetInt(MotorcycleRidingKey, 0) == 1)
        {
            if (playerMovement != null)
            {
                Mount();
            }
            else
            {
                Debug.LogError(
                    "MotorcycleController: PlayerMovement is not assigned.",
                    this
                );
            }
        }
    }

    private void OnApplicationQuit()
    {
        SaveMotorcycleState();
    }

    public void SaveMotorcycleState()
    {
        PlayerPrefs.SetFloat(MotorcycleXKey, transform.position.x);
        PlayerPrefs.SetFloat(MotorcycleYKey, transform.position.y);

        PlayerPrefs.SetFloat(MotorcycleHeadingXKey, heading.x);
        PlayerPrefs.SetFloat(MotorcycleHeadingYKey, heading.y);

        PlayerPrefs.SetInt(
            MotorcycleRidingKey,
            IsRiding ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        motorcycleCollider = GetComponent<BoxCollider2D>();

        heading = DirectionToVector(initialDirection);

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.bodyType = RigidbodyType2D.Static;

        UpdateSprite();
    }

    private void Update()
    {
        if (justMounted)
        {
            justMounted = false;
            UpdateSprite();
            return;
        }

        if (IsRiding && Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            Dismount();
            return;
        }

        UpdateSprite();
    }

    private void FixedUpdate()
    {
        if (!IsRiding)
            return;

        if (IsEngineStarting)
        {
            engineStartTimer -= Time.fixedDeltaTime;

            currentVelocity = Vector2.zero;
            rb.velocity = Vector2.zero;

            if (rider != null)
            {
                rider.position = new Vector3(
                    rb.position.x,
                    rb.position.y,
                    riderZ
                );
            }

            if (engineStartTimer <= 0f)
                IsEngineStarting = false;

            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        float x = 0f;
        float y = 0f;

        if (keyboard.aKey.isPressed)
            x -= 1f;

        if (keyboard.dKey.isPressed)
            x += 1f;

        if (keyboard.sKey.isPressed)
            y -= 1f;

        if (keyboard.wKey.isPressed)
            y += 1f;

        inputDirection = new Vector2(x, y);

        // Не позволяем диагонали двигаться быстрее.
        if (inputDirection.sqrMagnitude > 1f)
            inputDirection.Normalize();

        float dt = Time.fixedDeltaTime;

        if (inputDirection != Vector2.zero)
        {
            // Быстро меняем скорость в сторону нажатых клавиш.
            // Это уменьшает боковой занос при смене направления.
            float rate = acceleration;

            if (Vector2.Dot(currentVelocity, inputDirection) < 0f)
                rate = reverseAcceleration;

            currentVelocity = Vector2.MoveTowards(
                currentVelocity,
                inputDirection * maxSpeed,
                rate * dt
            );
        }
        else
        {
            // Быстро тормозим, когда клавиши отпущены.
            currentVelocity = Vector2.MoveTowards(
                currentVelocity,
                Vector2.zero,
                deceleration * dt
            );
        }

        float previousSpeed = rb.velocity.magnitude;
        SpeedChange = (currentVelocity.magnitude - previousSpeed)
            / Time.fixedDeltaTime;

        rb.velocity = currentVelocity;

        // Меняем направление спрайта, только если одна ось
        // заметно преобладает над другой.
        if (currentVelocity.sqrMagnitude > 0.25f)
        {
            float velocityX = Mathf.Abs(currentVelocity.x);
            float velocityY = Mathf.Abs(currentVelocity.y);

            if (velocityX > velocityY * 1.3f)
            {
                heading = currentVelocity.x > 0f
                    ? Vector2.right
                    : Vector2.left;
            }
            else if (velocityY > velocityX * 1.3f)
            {
                heading = currentVelocity.y > 0f
                    ? Vector2.up
                    : Vector2.down;
            }
        }

        // Персонаж остаётся на мотоцикле.
        if (rider != null)
        {
            rider.position = new Vector3(
                rb.position.x,
                rb.position.y,
                riderZ
            );
        }
    }

    public bool CanInteract()
    {
        return true;
    }

    public void Interact()
    {
        if (IsRiding)
        {
            Dismount();
            return;
        }

        if (playerMovement == null)
        {
            Debug.LogError(
                "MotorcycleController: PlayerMovement is not assigned.",
                this
            );
            return;
        }

        Mount();
    }

    private void Mount()
    {
        riderMovement = playerMovement;
        rider = riderMovement.transform;
        riderRb = rider.GetComponent<Rigidbody2D>();

        if (riderRb == null)
        {
            Debug.LogError(
                "The player needs a Rigidbody2D.",
                rider.gameObject
            );

            rider = null;
            riderMovement = null;
            return;
        }

        riderZ = rider.position.z;
        originalRiderBodyType = riderRb.bodyType;

        riderMovement.ResetMovement();
        riderMovement.enabled = false;

        riderRb.velocity = Vector2.zero;
        riderRb.bodyType = RigidbodyType2D.Kinematic;

        riderColliders = rider.GetComponentsInChildren<Collider2D>(true);
        originalColliderStates = new bool[riderColliders.Length];

        for (int i = 0; i < riderColliders.Length; i++)
        {
            Collider2D col = riderColliders[i];
            originalColliderStates[i] = col.enabled;

            if (!col.isTrigger)
                col.enabled = false;
        }

        riderRenderers = rider.GetComponentsInChildren<SpriteRenderer>(true);
        originalRendererStates = new bool[riderRenderers.Length];

        for (int i = 0; i < riderRenderers.Length; i++)
        {
            originalRendererStates[i] = riderRenderers[i].enabled;
            riderRenderers[i].enabled = false;
        }

        rider.position = new Vector3(
            rb.position.x,
            rb.position.y,
            riderZ
        );

        currentVelocity = Vector2.zero;
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Dynamic;

        // Отключаем детектор, чтобы E не обрабатывал другие объекты.
        InteractionDetector detector =
            rider.GetComponent<InteractionDetector>();

        if (detector != null)
            detector.enabled = false;

        IsRiding = true;
        IsEngineStarting = true;
        engineStartTimer = engineStartDuration;

        justMounted = true;

        UpdateSprite();
    }

    private void Dismount()
    {
        if (rider == null)
        {
            IsRiding = false;
            IsEngineStarting = false;
            currentVelocity = Vector2.zero;
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
            return;
        }

        IsRiding = false;
        IsEngineStarting = false;   
        currentVelocity = Vector2.zero;
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        Vector2 exitPosition = exitPoint != null
            ? (Vector2)exitPoint.position
            : rb.position + defaultExitOffset;

        rider.position = new Vector3(
            exitPosition.x,
            exitPosition.y,
            riderZ
        );

        if (riderColliders != null && originalColliderStates != null)
        {
            for (int i = 0; i < riderColliders.Length; i++)
            {
                if (riderColliders[i] != null)
                    riderColliders[i].enabled = originalColliderStates[i];
            }
        }

        if (riderRenderers != null && originalRendererStates != null)
        {
            for (int i = 0; i < riderRenderers.Length; i++)
            {
                if (riderRenderers[i] != null)
                    riderRenderers[i].enabled = originalRendererStates[i];
            }
        }

        if (riderRb != null)
        {
            riderRb.velocity = Vector2.zero;
            riderRb.bodyType = originalRiderBodyType;
        }

        if (riderMovement != null)
        {
            riderMovement.enabled = true;
            riderMovement.ResetMovement();

            InteractionDetector detector =
                riderMovement.GetComponent<InteractionDetector>();

            if (detector != null)
                detector.enabled = true;
        }

        rider = null;
        riderRb = null;
        riderMovement = null;

        riderColliders = null;
        originalColliderStates = null;
        riderRenderers = null;
        originalRendererStates = null;

        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null)
            return;

        Sprite selectedSprite;

        if (Mathf.Abs(heading.x) > Mathf.Abs(heading.y))
        {
            selectedSprite = heading.x > 0f
                ? (IsRiding ? rightWithPlayer : rightSprite)
                : (IsRiding ? leftWithPlayer : leftSprite);
        }
        else
        {
            selectedSprite = heading.y > 0f
                ? (IsRiding ? upWithPlayer : upSprite)
                : (IsRiding ? downWithPlayer : downSprite);
        }

        if (selectedSprite != null)
            spriteRenderer.sprite = selectedSprite;

        if (motorcycleCollider != null)
        {
            bool horizontal =
                Mathf.Abs(heading.x) > Mathf.Abs(heading.y);

            motorcycleCollider.size = horizontal
                ? horizontalColliderSize
                : verticalColliderSize;

            motorcycleCollider.offset = horizontal
                ? horizontalColliderOffset
                : verticalColliderOffset;
        }
    }

    private Vector2 DirectionToVector(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return Vector2.up;

            case Direction.Down:
                return Vector2.down;

            case Direction.Left:
                return Vector2.left;

            case Direction.Right:
                return Vector2.right;

            default:
                return Vector2.down;
        }
    }

    private void OnDisable()
    {
        if (IsRiding)
            Dismount();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsRiding)
            return;

        currentVelocity = Vector2.zero;
        rb.velocity = Vector2.zero;
    }
}
