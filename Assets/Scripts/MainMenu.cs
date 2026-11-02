
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Buttons")]
    [SerializeField] private Button[] buttons;

    [Header("Text Colors")]
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField]
    private Color selectedTextColor =
        new Color(0.75f, 0.9f, 0.55f);

    [Header("Scene")]
    [SerializeField] private int saveSelectSceneIndex = 1;

    private TMP_Text[] buttonTexts;
    private int selectedIndex;
    private bool loading;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogError("MainMenu: Buttons are not assigned.", this);
            return;
        }

        buttonTexts = new TMP_Text[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttonTexts[i] =
                    buttons[i].GetComponentInChildren<TMP_Text>(true);
        }

        selectedIndex = 0;
        UpdateTextColors();
        SelectButton();
    }


    private void Update()
    {
        if (loading || buttons == null || buttons.Length == 0)
            return;

        if (MenuInputSettings.WasPressed(MenuAction.Up) ||
            MenuInputSettings.WasPressed(MenuAction.Left))
        {
            MoveSelection(-1);
        }

        if (MenuInputSettings.WasPressed(MenuAction.Down) ||
            MenuInputSettings.WasPressed(MenuAction.Right))
        {
            MoveSelection(1);
        }

        if (MenuInputSettings.WasPressed(MenuAction.Confirm))
        {
            ActivateSelected();
        }

        if (MenuInputSettings.WasPressed(MenuAction.Back))
        {
            // Позже подключим возврат или закрытие текущего меню.
        }
    }


    private void MoveSelection(int direction)
    {
        int startIndex = selectedIndex;

        for (int i = 0; i < buttons.Length; i++)
        {
            selectedIndex =
                (selectedIndex + direction + buttons.Length)
                % buttons.Length;

            if (buttons[selectedIndex] != null &&
                buttons[selectedIndex].interactable &&
                buttons[selectedIndex].gameObject.activeInHierarchy)
            {
                UpdateTextColors();
                SelectButton();
                return;
            }
        }

        selectedIndex = startIndex;
    }

    private void UpdateTextColors()
    {
        if (buttonTexts == null)
            return;

        for (int i = 0; i < buttonTexts.Length; i++)
        {
            if (buttonTexts[i] == null)
                continue;

            buttonTexts[i].color =
                i == selectedIndex
                    ? selectedTextColor
                    : normalTextColor;
        }
    }

    private void SelectButton()
    {
        if (buttons[selectedIndex] == null)
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(
                buttons[selectedIndex].gameObject
            );
    }

    private void ActivateSelected()
    {
        if (buttons[selectedIndex] == null ||
            !buttons[selectedIndex].interactable)
            return;

        buttons[selectedIndex].onClick.Invoke();
    }

    public void PlayGame()
    {
        if (loading)
            return;

        loading = true;
        SceneManager.LoadSceneAsync(saveSelectSceneIndex);
    }

    public void OpenSettings()
    {
        Debug.Log("Open Settings");
    }

    public void OpenAchievements()
    {
        Debug.Log("Open Achievements");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit Game");
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
