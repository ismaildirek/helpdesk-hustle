using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-2000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class BossIntroDialogue : MonoBehaviour
{
    [Header("Dialogue View")]
    [SerializeField] private TextMesh dialogueText;
    [SerializeField] private TextMesh progressText;

    [Header("Dialogue Pages")]
    [SerializeField, TextArea(3, 8)] private string[] pages =
        CreateDefaultPages();
    [SerializeField, Min(0f)] private float inputDelay = 0.2f;

    private static bool introAlreadyShown;

    private int currentPageIndex;
    private float inputAllowedAt;
    private float previousTimeScale;
    private bool presenting;

    public static bool IsBlockingOfficeInput { get; private set; }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        introAlreadyShown = false;
        IsBlockingOfficeInput = false;
    }

    public static void BeginNewGame()
    {
        introAlreadyShown = false;
        IsBlockingOfficeInput = false;
        TaskMissionSession.BeginNewGame();
        BossAngerSession.BeginNewGame();
        SurvivalTimeSession.BeginNewGame();
        DailyOfficeEventSession.BeginNewGame();
        GameProgressionSession.BeginNewGame();
        GamePauseSession.BeginNewGame();
    }

    private void Awake()
    {
        if (introAlreadyShown)
        {
            gameObject.SetActive(false);
            return;
        }

        if (!HasValidView())
        {
            Debug.LogError(
                "Boss intro dialogue needs its text references and pages.",
                this);
            gameObject.SetActive(false);
            return;
        }

        introAlreadyShown = true;
        presenting = true;
        IsBlockingOfficeInput = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        inputAllowedAt = Time.unscaledTime + inputDelay;
        ShowPage(0);
    }

    private void Update()
    {
        if (!presenting ||
            Time.unscaledTime < inputAllowedAt ||
            !WasPointerPressed())
        {
            return;
        }

        int nextPageIndex = currentPageIndex + 1;
        if (nextPageIndex >= pages.Length)
        {
            FinishIntro();
            return;
        }

        ShowPage(nextPageIndex);
    }

    private void OnDisable()
    {
        if (presenting)
            StopPresenting();
    }

    private void OnDestroy()
    {
        if (presenting)
            StopPresenting();
    }

    private bool HasValidView()
    {
        return dialogueText != null &&
               progressText != null &&
               pages != null &&
               pages.Length > 0;
    }

    private void ShowPage(int pageIndex)
    {
        currentPageIndex = pageIndex;
        dialogueText.text = pages[currentPageIndex];

        bool isLastPage = currentPageIndex == pages.Length - 1;
        string action = isLastPage
            ? "CLICK TO START"
            : "CLICK TO CONTINUE";
        progressText.text =
            $"{currentPageIndex + 1} / {pages.Length}     {action}";
    }

    private void FinishIntro()
    {
        StopPresenting();
        gameObject.SetActive(false);
    }

    private void StopPresenting()
    {
        presenting = false;
        IsBlockingOfficeInput = false;
        Time.timeScale = previousTimeScale;
    }

    private static bool WasPointerPressed()
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
    }

    public static string[] CreateDefaultPages()
    {
        return new[]
        {
            "Welcome to the new office!\n" +
            "It looks peaceful, but the computers\n" +
            "strongly disagree.",

            "Check the task card regularly.\n" +
            "It shows the problem, floor, room,\n" +
            "and remaining time.\n\n" +
            "Find the correct floor and room.\n" +
            "Then complete the mini-game to fix it.\n\n" +
            "Wrong room? People will stare.\n" +
            "The problem stays, but the\n" +
            "embarrassment is free.",

            "Every task has a time limit.\n" +
            "Finish it in time, and a new task\n" +
            "will appear.\n\n" +
            "An unfinished task does not count.\n" +
            "Sadly, printers never repair\n" +
            "themselves.",

            "The bar shows how much patience\n" +
            "I have left. Ignoring tasks will\n" +
            "make it go down.\n\n" +
            "As the bar drops, my face gets angrier.\n" +
            "Do not take it personally...\n" +
            "actually, you probably should.",

            "Wake up the modems, remove viruses,\n" +
            "fix the cables, and stop suspicious\n" +
            "emails before they cause trouble.\n\n" +
            "Complete the tasks and do not empty\n" +
            "my patience bar. Save the office,\n" +
            "and I may consider a coffee break...\n" +
            "may."
        };
    }

#if UNITY_EDITOR
    public void ConfigureEditor(
        TextMesh configuredDialogueText,
        TextMesh configuredProgressText,
        string[] configuredPages)
    {
        dialogueText = configuredDialogueText;
        progressText = configuredProgressText;
        pages = configuredPages;
    }
#endif
}
