using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Presents a short, touch-friendly office tutorial. The overlay is created at
/// runtime so it stays consistent with the other generated HUD surfaces.
/// </summary>
internal sealed class OfficeHelpOverlay : MonoBehaviour
{
    private const string OfficeSceneName = "YeniOfis";
    private const string HelpSeenKey = "OfficeGame.HelpTutorialSeenV1";

    private static OfficeHelpOverlay instance;
    private static float inputBlockedUntil;

    private readonly string[] pageTitles =
    {
        "FOLLOW THE TASK CARD",
        "STAY EMPLOYED",
        "OFFICE TOOLS",
        "BEST RECORDS"
    };

    private readonly string[] pageBodies =
    {
        "1. OPEN THE TASK CARD.\n\n" +
        "2. CHECK THE FLOOR AND ROOM NUMBER.\n\n" +
        "3. OPEN THE FLOOR MAP AND ENTER THAT ROOM.\n\n" +
        "4. FINISH THE MINI GAME BEFORE TIME RUNS OUT.",

        "SUCCESSFUL TASKS BUILD YOUR COMBO AND SCORE.\n\n" +
        "EVERY THIRD COMBO CAN CALM THE BOSS.\n\n" +
        "MISSED TASKS RAISE THE BOSS'S ANGER.\n\n" +
        "WHEN THE ANGER BAR FILLS, YOUR SHIFT IS OVER.",

        "PAUSE STOPS THE SHIFT AND TASK TIMERS.\n\n" +
        "THE SPEAKER ICON MUTES OR RESTORES ALL AUDIO.\n\n" +
        "THE QUESTION MARK OPENS THIS GUIDE AGAIN.\n\n" +
        "LEAVING A MINI GAME DOES NOT COMPLETE THE TASK.",

        string.Empty
    };

    private GameObject overlayRoot;
    private RectTransform frame;
    private RectTransform backButton;
    private RectTransform nextButton;
    private RectTransform closeButton;
    private Text pageTitle;
    private Text pageBody;
    private Text pageIndicator;
    private Text nextLabel;
    private Font displayFont;
    private int pageIndex;
    private bool isOpen;
    private bool wasPausedBeforeOpen;

    public static bool IsBlockingOfficeInput =>
        (instance != null && instance.isOpen) ||
        Time.unscaledTime < inputBlockedUntil;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        instance = null;
        inputBlockedUntil = 0f;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterInstaller()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    public static void Show()
    {
        if (instance != null)
            instance.Open();
    }

    internal static bool CloseFromSystemBack()
    {
        if (instance == null || !instance.isOpen)
            return false;

        instance.Close();
        return true;
    }

    private static void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (scene.name != OfficeSceneName)
            return;

        GameObject root = new(
            "Office Help Guide",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        root.AddComponent<OfficeHelpOverlay>();
    }

    private void Awake()
    {
        instance = this;
        ConfigureCanvas();
        LoadFont();
        BuildOverlay();
    }

    private IEnumerator Start()
    {
        if (PlayerPrefs.GetInt(HelpSeenKey, 0) != 0)
            yield break;

        while (BossIntroDialogue.IsBlockingOfficeInput)
            yield return null;

        yield return new WaitForSecondsRealtime(0.45f);
        Open();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        if (isOpen && !wasPausedBeforeOpen)
            GamePauseSession.SetPaused(false);
    }

    private void Update()
    {
        if (!isOpen || !TryReadPointerPress(out Vector2 position))
            return;

        if (Contains(closeButton, position))
        {
            Close();
            return;
        }

        if (pageIndex > 0 && Contains(backButton, position))
        {
            PlayClick(-0.02f);
            pageIndex--;
            RefreshPage();
            return;
        }

        if (!Contains(nextButton, position))
            return;

        PlayClick(0.025f);
        if (pageIndex >= pageTitles.Length - 1)
        {
            Close(false);
            return;
        }

        pageIndex++;
        RefreshPage();
    }

    private void Open()
    {
        if (isOpen)
            return;

        pageIndex = 0;
        isOpen = true;
        wasPausedBeforeOpen = GamePauseSession.IsPaused;
        if (!wasPausedBeforeOpen)
            GamePauseSession.SetPaused(true);

        overlayRoot.SetActive(true);
        RefreshPage();
        StartCoroutine(MiniGameJuice.PopIn(
            frame,
            Vector3.one,
            0.3f,
            1.08f));
    }

    private void Close(bool playSound = true)
    {
        if (!isOpen)
            return;

        if (playSound)
            PlayClick(-0.01f);

        PlayerPrefs.SetInt(HelpSeenKey, 1);
        PlayerPrefs.Save();
        isOpen = false;
        overlayRoot.SetActive(false);
        inputBlockedUntil = Time.unscaledTime + 0.12f;

        if (!wasPausedBeforeOpen)
            GamePauseSession.SetPaused(false);
    }

    private void RefreshPage()
    {
        pageTitle.text = pageTitles[pageIndex];
        pageBody.text = pageIndex == pageTitles.Length - 1
            ? BuildBestRecordsPage()
            : pageBodies[pageIndex];
        pageIndicator.text = $"GUIDE  {pageIndex + 1} / {pageTitles.Length}";
        nextLabel.text = pageIndex == pageTitles.Length - 1
            ? "GOT IT"
            : "NEXT";
        backButton.gameObject.SetActive(pageIndex > 0);

        StartCoroutine(MiniGameJuice.PunchScale(
            pageBody.rectTransform,
            Vector3.one,
            0.035f,
            0.16f));
    }

    private static string BuildBestRecordsPage()
    {
        GameProgressionSession.EnsureRunStarted();
        return
            $"BEST SCORE\n{GameProgressionSession.BestScore:000000}\n\n" +
            $"LONGEST SHIFT\n{FormatRecordTime(GameProgressionSession.BestTime)}\n\n" +
            $"MOST TASKS COMPLETED\n{GameProgressionSession.BestCompletedTasks:00}\n\n" +
            $"CURRENT SCORE\n{GameProgressionSession.Score:000000}";
    }

    private static string FormatRecordTime(float totalSeconds)
    {
        int seconds = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1400;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void LoadFont()
    {
        GamePresentationLibrary library =
            Resources.Load<GamePresentationLibrary>(
                "GamePresentationLibrary");
        displayFont = library != null
            ? library.DisplayFont
            : null;
        if (displayFont == null)
        {
            displayFont = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
        }
    }

    private void BuildOverlay()
    {
        overlayRoot = new GameObject("Overlay", typeof(RectTransform));
        overlayRoot.transform.SetParent(transform, false);
        Stretch((RectTransform)overlayRoot.transform, Vector2.zero);

        Image dimmer = CreateImage(
            "Dimmer",
            overlayRoot.transform,
            new Color(0.005f, 0.015f, 0.03f, 0.9f));
        Stretch(dimmer.rectTransform, Vector2.zero);

        Image frameImage = CreateImage(
            "GuideFrame",
            overlayRoot.transform,
            new Color(0.04f, 0.52f, 0.68f, 1f));
        frame = frameImage.rectTransform;
        SetRect(
            frame,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(870f, 1260f),
            Vector2.zero,
            new Vector2(0.5f, 0.5f));

        Image panel = CreateImage(
            "GuidePanel",
            frame,
            new Color(0.018f, 0.105f, 0.17f, 1f));
        Stretch(panel.rectTransform, new Vector2(12f, 12f));

        Image headerBand = CreateImage(
            "HeaderBand",
            panel.transform,
            new Color(0.025f, 0.22f, 0.32f, 1f));
        SetRect(
            headerBand.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(820f, 200f),
            new Vector2(0f, -100f),
            new Vector2(0.5f, 0.5f));

        Text title = CreateText(
            "GuideTitle",
            panel.transform,
            51,
            TextAnchor.MiddleCenter,
            new Color(1f, 0.84f, 0.25f, 1f));
        SetRect(
            title.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(690f, 95f),
            new Vector2(0f, -72f),
            new Vector2(0.5f, 0.5f));
        title.text = "HOW TO SURVIVE YOUR SHIFT";

        pageIndicator = CreateText(
            "PageIndicator",
            panel.transform,
            25,
            TextAnchor.MiddleCenter,
            new Color(0.48f, 0.88f, 1f, 1f));
        SetRect(
            pageIndicator.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(520f, 50f),
            new Vector2(0f, -151f),
            new Vector2(0.5f, 0.5f));

        closeButton = CreateButton(
            panel.transform,
            "X",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(78f, 78f),
            new Vector2(-52f, -50f),
            new Color(0.72f, 0.16f, 0.13f, 1f),
            out _);

        Image content = CreateImage(
            "Content",
            panel.transform,
            new Color(0.008f, 0.055f, 0.095f, 0.95f));
        SetRect(
            content.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(730f, 720f),
            new Vector2(0f, -590f),
            new Vector2(0.5f, 0.5f));

        Outline contentOutline = content.gameObject.AddComponent<Outline>();
        contentOutline.effectColor =
            new Color(0.08f, 0.38f, 0.52f, 0.7f);
        contentOutline.effectDistance = new Vector2(2f, -2f);

        pageTitle = CreateText(
            "PageTitle",
            content.transform,
            38,
            TextAnchor.MiddleCenter,
            new Color(0.36f, 0.88f, 1f, 1f));
        SetRect(
            pageTitle.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(660f, 90f),
            new Vector2(0f, -67f),
            new Vector2(0.5f, 0.5f));

        pageBody = CreateText(
            "PageBody",
            content.transform,
            32,
            TextAnchor.UpperLeft,
            new Color(0.92f, 0.98f, 1f, 1f));
        SetRect(
            pageBody.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(620f, 540f),
            new Vector2(0f, -390f),
            new Vector2(0.5f, 0.5f));
        pageBody.lineSpacing = 1.12f;

        backButton = CreateButton(
            panel.transform,
            "BACK",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(300f, 106f),
            new Vector2(-188f, -500f),
            new Color(0.12f, 0.34f, 0.5f, 1f),
            out _);
        nextButton = CreateButton(
            panel.transform,
            "NEXT",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(300f, 106f),
            new Vector2(188f, -500f),
            new Color(0.08f, 0.66f, 0.42f, 1f),
            out nextLabel);

        Text footer = CreateText(
            "Footer",
            panel.transform,
            22,
            TextAnchor.MiddleCenter,
            new Color(0.5f, 0.68f, 0.78f, 1f));
        SetRect(
            footer.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(720f, 44f),
            new Vector2(0f, 28f),
            new Vector2(0.5f, 0.5f));
        footer.text = "THE SHIFT TIMER IS PAUSED WHILE THIS GUIDE IS OPEN";

        overlayRoot.SetActive(false);
    }

    private RectTransform CreateButton(
        Transform parent,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 size,
        Vector2 position,
        Color color,
        out Text labelText)
    {
        Image image = CreateImage(label, parent, color);
        SetRect(
            image.rectTransform,
            anchorMin,
            anchorMax,
            size,
            position,
            new Vector2(0.5f, 0.5f));

        Shadow shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0.02f, 0.04f, 0.85f);
        shadow.effectDistance = new Vector2(0f, -7f);

        labelText = CreateText(
            $"{label} Label",
            image.transform,
            34,
            TextAnchor.MiddleCenter,
            Color.white);
        Stretch(labelText.rectTransform, new Vector2(10f, 6f));
        labelText.text = label;
        return image.rectTransform;
    }

    private Text CreateText(
        string objectName,
        Transform parent,
        int fontSize,
        TextAnchor alignment,
        Color color)
    {
        GameObject textObject = new(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = displayFont;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.025f, 0.06f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private static Image CreateImage(
        string objectName,
        Transform parent,
        Color color)
    {
        GameObject imageObject = new(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 size,
        Vector2 position,
        Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect, Vector2 inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = inset;
        rect.offsetMax = -inset;
        rect.localScale = Vector3.one;
    }

    private static bool Contains(RectTransform rect, Vector2 position)
    {
        return rect != null &&
            rect.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(
                rect,
                position,
                null);
    }

    private static void PlayClick(float pitchOffset)
    {
        ProceduralGameAudio.Play(GameSound.UiClick, pitchOffset);
    }

    private static bool TryReadPointerPress(out Vector2 position)
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        position = Vector2.zero;
        return false;
    }
}
