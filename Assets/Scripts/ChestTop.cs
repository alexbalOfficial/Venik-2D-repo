using UnityEngine;

public class ChestTop : MonoBehaviour
{
    [SerializeField] private Chest chest;

    public Sprite openedSprite;
    public Sprite closedSprite;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (chest)
            chest.OnChestStateChanged += UpdateVisual;
    }

    void OnDisable()
    {
        if (chest)
            chest.OnChestStateChanged -= UpdateVisual;
    }

    void Start()
    {
        if (chest)
            UpdateVisual(chest.IsOpened);
    }

    private void UpdateVisual(bool opened)
    {
        sr.sprite = opened ? openedSprite : closedSprite;
    }
}
