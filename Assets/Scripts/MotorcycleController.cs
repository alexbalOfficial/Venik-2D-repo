
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
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
    [SerializeField] private float maxSpeed = 5.5f;
    [SerializeField] private float acceleration = 3f;
    [SerializeField] private float braking = 5f;
    [SerializeField] private float naturalDeceleration = 1.5f;
    [SerializeField] private float turnSpeed = 120f;

    [Header("Turning")]
    [SerializeField, Range(0f, 1f)]
    private float turningAtZeroSpeed = 0.25f;

    [Header("Dismount")]
    [SerializeField]
    private Vector2 defaultExitOffset =
        new Vector2(1.2f, 0f);

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private Vector2 heading;
    private float currentSpeed;
    private bool isRiding;

    private Transform rider;
    private Rigidbody2D riderRb;
    private RigidbodyType2D originalRiderBodyType;
    private float riderZ;

    private PlayerMovement riderMovement;
    private Collider2D[] riderColliders;
    private bool[] originalColliderStates;

    private SpriteRenderer[] riderRenderers;
    private bool[] originalRendererStates;

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
        UpdateSprite();
        
    }

    private void FixedUpdate()
    {
        if (!isRiding)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        float dt = Time.fixedDeltaTime;

        bool accelerating = keyboard.wKey.isPressed;
        bool brakingNow = keyboard.sKey.isPressed;

        float turnInput = 0f;

        if (keyboard.aKey.isPressed)
            turnInput += 1f;

        if (keyboard.dKey.isPressed)
            turnInput -= 1f;

        // Turn the motorcycle gradually.
        float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed);
        float turnFactor = Mathf.Lerp(
            turningAtZeroSpeed,
            1f,
            speedFactor
        );

        float turnAngle = turnInput * turnSpeed *
                          turnFactor * dt;

        heading = Quaternion.Euler(0f, 0f, turnAngle) * heading;
        heading.Normalize();

        // W accelerates forward.
        if (accelerating && !brakingNow)
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                maxSpeed,
                acceleration * dt
            );
        }
        // S brakes without reversing.
        else if (brakingNow)
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                0f,
                braking * dt
            );
        }
        // Gradual slowdown when no key is pressed.
        else
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                0f,
                naturalDeceleration * dt
            );
        }

        rb.velocity = heading * currentSpeed;

        // Keep the actual player at the motorcycle position.
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
        if (isRiding)
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

        // Запоминаем и отключаем обычное управление игроком.
        riderMovement.ResetMovement();
        riderMovement.enabled = false;

        riderRb.velocity = Vector2.zero;
        riderRb.bodyType = RigidbodyType2D.Kinematic;

        // Сохраняем состояние коллайдеров игрока.
        riderColliders =
            rider.GetComponentsInChildren<Collider2D>(true);

        originalColliderStates =
            new bool[riderColliders.Length];

        for (int i = 0; i < riderColliders.Length; i++)
        {
            Collider2D col = riderColliders[i];

            originalColliderStates[i] = col.enabled;

            if (!col.isTrigger)
                col.enabled = false;
        }

        // Сохраняем состояние спрайтов и скрываем игрока.
        riderRenderers =
            rider.GetComponentsInChildren<SpriteRenderer>(true);

        originalRendererStates =
            new bool[riderRenderers.Length];

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

        currentSpeed = 0f;
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Dynamic;

        isRiding = true;


        UpdateSprite();
    }

    private void Dismount()
    {
        if (rider == null)
        {
            isRiding = false;
            rb.bodyType = RigidbodyType2D.Static;
            return;
        }

        isRiding = false;
        currentSpeed = 0f;
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

        // Возвращаем коллайдеры игрока.
        if (riderColliders != null && originalColliderStates != null)
        {
            for (int i = 0; i < riderColliders.Length; i++)
            {
                if (riderColliders[i] != null)
                    riderColliders[i].enabled =
                        originalColliderStates[i];
            }
        }

        // Возвращаем отображение игрока.
        if (riderRenderers != null && originalRendererStates != null)
        {
            for (int i = 0; i < riderRenderers.Length; i++)
            {
                if (riderRenderers[i] != null)
                    riderRenderers[i].enabled =
                        originalRendererStates[i];
            }
        }

        // Возвращаем физику и управление.
        if (riderRb != null)
        {
            riderRb.velocity = Vector2.zero;
            riderRb.bodyType = originalRiderBodyType;
        }

        if (riderMovement != null)
        {
            riderMovement.enabled = true;
            riderMovement.ResetMovement();
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

        // Выбираем спрайт по направлению мотоцикла,
        // а не по текущему вектору скорости.
        Vector2 direction = heading;

        Sprite selectedSprite;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            selectedSprite = direction.x > 0f
                ? (isRiding ? rightWithPlayer : rightSprite)
                : (isRiding ? leftWithPlayer : leftSprite);
        }
        else
        {
            selectedSprite = direction.y > 0f
                ? (isRiding ? upWithPlayer : upSprite)
                : (isRiding ? downWithPlayer : downSprite);
        }

        if (selectedSprite != null)
            spriteRenderer.sprite = selectedSprite;
        if (motorcycleCollider != null)
        {
            bool horizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);

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
        // Avoid leaving the player hidden if this component
        // is disabled while someone is riding.                                                                                                                                                                             
        if (isRiding)
            Dismount();
    }
}

