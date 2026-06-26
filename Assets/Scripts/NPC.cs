using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPC : MonoBehaviour, IInteractable
{
    [Header("Default Dialogue")]
    public NPCDialogue dialogueData;

    [Header("Conditional Dialogues")]
    public DialogueCondition[] conditionalDialogues;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public TMP_Text nameText;
    public Image portraitImage;

    private int dialogueIndex;
    private bool isTyping;
    private bool isDialogueActive;

    private NPCDialogue currentDialogue;

    public bool CanInteract()
    {
        return !isDialogueActive;
    }

    public void Interact()
    {
        if ((PauseController.IsGamePaused && !isDialogueActive))
            return;

        if (isDialogueActive)
        {
            NextLine();
        }
        else
        {
            StartDialogue();
        }
    }

    private NPCDialogue GetCurrentDialogue()
    {
        NPCDialogue bestDialogue = dialogueData;
        int bestPriority = -1;

        foreach (var condition in conditionalDialogues)
        {
            if (condition.dialogue == null)
                continue;

            if (!GameFlags.Get(condition.requiredFlag))
                continue;

            if (condition.priority > bestPriority)
            {
                bestPriority = condition.priority;
                bestDialogue = condition.dialogue;
            }
        }

        return bestDialogue;
    }

    void StartDialogue()
    {
        currentDialogue = GetCurrentDialogue();

        if (currentDialogue == null)
            return;

        isDialogueActive = true;
        dialogueIndex = 0;

        nameText.SetText(currentDialogue.npcName);
        portraitImage.sprite = currentDialogue.npcPortrait;

        dialoguePanel.SetActive(true);
        PauseController.SetPause(true);

        StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();

            dialogueText.SetText(
                currentDialogue.dialogueLines[dialogueIndex]
            );

            isTyping = false;
        }
        else if (++dialogueIndex < currentDialogue.dialogueLines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.SetText("");

        foreach (char letter in currentDialogue.dialogueLines[dialogueIndex])
        {
            dialogueText.text += letter;

            SoundEffectManager.PlayVoice(
                currentDialogue.voiceSound,
                currentDialogue.voicePitch
            );

            yield return new WaitForSeconds(
                currentDialogue.typingSpeed
            );
        }

        isTyping = false;

        if (currentDialogue.autoProgressLines.Length > dialogueIndex &&
            currentDialogue.autoProgressLines[dialogueIndex])
        {
            yield return new WaitForSeconds(
                currentDialogue.autoProgressDelay
            );

            NextLine();
        }
    }

    public void EndDialogue()
    {
        StopAllCoroutines();

        isDialogueActive = false;

        dialogueText.SetText("");

        dialoguePanel.SetActive(false);

        PauseController.SetPause(false);
    }
}