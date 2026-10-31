
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class SaveSelectionMenu : MonoBehaviour
{
    [Header("Save Slots")]
    [SerializeField] private Button[] slotButtons;

    [Header("Text Colors")]
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField]
    private Color selectedTextColor =
        new Color(0.75f, 0.9f, 0.55f);

    [Header("New Game")]
    [SerializeField] private string newGameSceneName = "Game";

    private TMP_Text[] slotTexts;
    private int selectedIndex;
    private bool loading;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (slotButtons == null ||
            slotButtons.Length != SaveSystem.SlotCount)
        {
            Debug.LogError("В SaveSelectionMenu нужно назначить ровно 8 кнопок.");
            enabled = false;
            return;
        }

        slotTexts = new TMP_Text[slotButtons.Length];

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null)
                continue;

            // Отключаем взаимодействие мышью.
            // Выбор и активация выполняются этим скриптом.
            slotButtons[i].interactable = false;

            slotTexts[i] =
                slotButtons[i].GetComponentInChildren<TMP_Text>(true);
        }

        selectedIndex = 0;

        RefreshSlotLabels();
        UpdateTextColors();
    }

    private void Update()
    {
        if (loading || Keyboard.current == null)
            return;

        if (Keyboard.current.wKey.wasPressedThisFrame)
            MoveSelection(-1);

        if (Keyboard.current.sKey.wasPressedThisFrame)
            MoveSelection(1);

        if (Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            SelectSlot();
        }
    }

    private void MoveSelection(int direction)
    {
        selectedIndex =
            (selectedIndex + direction + slotButtons.Length)
            % slotButtons.Length;

        UpdateTextColors();
    }

    private void UpdateTextColors()
    {
        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] == null)
                continue;

            slotTexts[i].color =
                i == selectedIndex
                ? selectedTextColor
                : normalTextColor;
        }
    }

    private void RefreshSlotLabels()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotTexts[i] == null)
                continue;

            SaveData data = SaveSystem.Load(i);

            if (data == null)
            {
                slotTexts[i].text =
                    "Слот " + (i + 1) + "\nПусто";
            }
            else
            {
                string date = string.IsNullOrEmpty(data.savedAt)
                    ? "Дата неизвестна"
                    : data.savedAt;

                slotTexts[i].text =
                    "Слот " + (i + 1) + "\n" + date;
            }
        }
    }

    private void SelectSlot()
    {
        if (loading)
            return;

        loading = true;

        SaveSystem.SetCurrentSlot(selectedIndex);

        SaveData data = SaveSystem.Load(selectedIndex);

        if (data != null && !string.IsNullOrEmpty(data.sceneName))
        {
            // Продолжаем сохранённое прохождение.
            SceneManager.LoadSceneAsync(data.sceneName);
        }
        else
        {
            // Начинаем новое прохождение в пустом слоте.
            if (string.IsNullOrWhiteSpace(newGameSceneName))
            {
                Debug.LogError("Не указано имя стартовой сцены.");
                loading = false;
                return;
            }

            SceneManager.LoadSceneAsync(newGameSceneName);
        }
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
