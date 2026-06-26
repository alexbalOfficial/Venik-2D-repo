using UnityEngine;
using System;

public class Chest : MonoBehaviour, IInteractable
{
    public bool IsOpened { get; private set; }
    public string ChestID { get; private set; }

    [Header("Center Part")]
    public Sprite openedSprite;
    public Sprite closedSprite;

    [Header("Bottom Part")]
    public SpriteRenderer bottomPartRenderer;
    public Sprite openedBottomSprite;
    public Sprite closedBottomSprite;
    public GameObject bottomPartObject;

    public event Action<bool> OnChestStateChanged;

    private BoxCollider2D boxCollider;
    private Vector2 closedSize;
    private Vector2 closedOffset;
    private SpriteRenderer centerRenderer;

    private const float OpenedWidth = 3f / 16f;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        centerRenderer = GetComponent<SpriteRenderer>();

        closedSize = boxCollider.size;
        closedOffset = boxCollider.offset;
    }

    void Start()
    {
        ChestID = GlobalHelper.GenerateUniqueID(gameObject);
        SetOpened(false);
    }

    public bool CanInteract() => true;

    public void Interact()
    {
        SetOpened(!IsOpened);
        SoundEffectManager.Play("kalitka");
    }

    private void SetOpened(bool opened)
    {
        IsOpened = opened;
        
        // Центр
        if (centerRenderer != null)
            centerRenderer.sprite = IsOpened ? openedSprite : closedSprite;

        // Низ
        if (bottomPartRenderer != null)
            bottomPartRenderer.sprite = IsOpened ? openedBottomSprite : closedBottomSprite;

        // Коллизия нижней части
        if (bottomPartObject != null)
        {
            Collider2D col = bottomPartObject.GetComponent<Collider2D>();

            if (col != null)
                col.enabled = IsOpened;
        }

        UpdateHitbox();

        OnChestStateChanged?.Invoke(IsOpened);
    }

    private void UpdateHitbox()
    {
        if (!IsOpened)
        {
            boxCollider.size = closedSize;
            boxCollider.offset = closedOffset;
            return;
        }

        boxCollider.size = new Vector2(
            OpenedWidth,
            closedSize.y
        );

        float leftEdge = closedOffset.x - closedSize.x / 2f;
        float newOffsetX = leftEdge + OpenedWidth / 2f;

        boxCollider.offset = new Vector2(
            newOffsetX,
            closedOffset.y
        );
    }
}