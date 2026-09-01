using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum DailyOfficeEventType
{
    None,
    NetworkTrouble,
    BossInspection,
    CoffeeBoost
}

public readonly struct DailyOfficeEventInfo
{
    public DailyOfficeEventInfo(
        DailyOfficeEventType type,
        string title,
        string description,
        string effectText,
        float taskDurationMultiplier,
        float scoreMultiplier,
        Color accentColor)
    {
        Type = type;
        Title = title;
        Description = description;
        EffectText = effectText;
        TaskDurationMultiplier = taskDurationMultiplier;
        ScoreMultiplier = scoreMultiplier;
        AccentColor = accentColor;
    }

    public DailyOfficeEventType Type { get; }
    public string Title { get; }
    public string Description { get; }
    public string EffectText { get; }
    public float TaskDurationMultiplier { get; }
    public float ScoreMultiplier { get; }
    public Color AccentColor { get; }
}

/// <summary>
/// Owns the current day modifier. Gameplay reads the multipliers while the
/// presenter consumes only the announcement, keeping rules and UI separate.
/// </summary>
public static class DailyOfficeEventSession
{
    private static DailyOfficeEventType currentType;
    private static int observedAbsoluteDay = 1;
    private static int announcementVersion;
    private static int consumedAnnouncementVersion;

    public static event Action Changed;

    public static DailyOfficeEventType CurrentType => currentType;
    public static DailyOfficeEventInfo CurrentInfo => GetInfo(currentType);
    public static float TaskDurationMultiplier =>
        CurrentInfo.TaskDurationMultiplier;
    public static float ScoreMultiplier => CurrentInfo.ScoreMultiplier;
    public static int AngerPerFailure => currentType ==
        DailyOfficeEventType.BossInspection
            ? 2
            : 1;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        currentType = DailyOfficeEventType.None;
        observedAbsoluteDay = 1;
        announcementVersion = 0;
        consumedAnnouncementVersion = 0;
        Changed = null;
    }

    public static void BeginNewGame()
    {
        currentType = DailyOfficeEventType.None;
        observedAbsoluteDay = 1;
        announcementVersion = 0;
        consumedAnnouncementVersion = 0;
        Changed?.Invoke();
    }

    public static DailyOfficeEventType SelectNextEvent(
        DailyOfficeEventType previous,
        int randomIndex)
    {
        DailyOfficeEventType[] allEvents =
        {
            DailyOfficeEventType.NetworkTrouble,
            DailyOfficeEventType.BossInspection,
            DailyOfficeEventType.CoffeeBoost
        };

        DailyOfficeEventType[] choices = previous ==
            DailyOfficeEventType.None
                ? allEvents
                : Array.FindAll(
                    allEvents,
                    item => item != previous);
        int safeIndex = Mathf.Abs(randomIndex % choices.Length);
        return choices[safeIndex];
    }

    public static DailyOfficeEventInfo GetInfo(
        DailyOfficeEventType type)
    {
        return type switch
        {
            DailyOfficeEventType.NetworkTrouble =>
                new DailyOfficeEventInfo(
                    type,
                    "NETWORK TROUBLE",
                    "THE NETWORK IS CRAWLING TODAY.",
                    "NEW TASK TIME  -15%",
                    0.85f,
                    1f,
                    new Color(0.2f, 0.7f, 1f, 1f)),
            DailyOfficeEventType.BossInspection =>
                new DailyOfficeEventInfo(
                    type,
                    "BOSS INSPECTION",
                    "THE BOSS IS WATCHING EVERY CLICK.",
                    "MISSED TASK ANGER  x2     SCORE  +25%",
                    1f,
                    1.25f,
                    new Color(1f, 0.28f, 0.2f, 1f)),
            DailyOfficeEventType.CoffeeBoost =>
                new DailyOfficeEventInfo(
                    type,
                    "COFFEE BOOST",
                    "FRESH COFFEE. QUESTIONABLE CONFIDENCE.",
                    "NEW TASK TIME  +15%     SCORE  +10%",
                    1.15f,
                    1.1f,
                    new Color(1f, 0.72f, 0.2f, 1f)),
            _ => new DailyOfficeEventInfo(
                DailyOfficeEventType.None,
                "NORMAL SHIFT",
                "NO SPECIAL OFFICE EVENT.",
                "STANDARD TASK TIME AND SCORE",
                1f,
                1f,
                new Color(0.45f, 0.85f, 1f, 1f))
        };
    }

    internal static void Tick(float elapsedSeconds)
    {
        if (!SurvivalTimeSession.HasStarted || BossAngerSession.HasLost)
            return;

        int absoluteDay = Mathf.Max(
            1,
            Mathf.FloorToInt(
                Mathf.Max(0f, elapsedSeconds) /
                SurvivalTimeSession.DayDurationSeconds) + 1);
        if (absoluteDay <= observedAbsoluteDay)
            return;

        observedAbsoluteDay = absoluteDay;
        DailyOfficeEventType selected = SelectNextEvent(
            currentType,
            UnityEngine.Random.Range(0, int.MaxValue));
        Activate(selected);
    }

    internal static bool TryConsumeAnnouncement(
        out DailyOfficeEventInfo info)
    {
        if (currentType == DailyOfficeEventType.None ||
            consumedAnnouncementVersion == announcementVersion)
        {
            info = default;
            return false;
        }

        consumedAnnouncementVersion = announcementVersion;
        info = CurrentInfo;
        return true;
    }

#if UNITY_EDITOR
    public static void ActivateForTests(DailyOfficeEventType type)
    {
        Activate(type);
    }
#endif

    private static void Activate(DailyOfficeEventType type)
    {
        currentType = type;
        announcementVersion++;
        Changed?.Invoke();
    }
}

/// <summary>
/// Displays non-blocking event cards in the office and floor map. Events that
/// start during a mini-game remain pending until one of those scenes opens.
/// </summary>
internal sealed class DailyOfficeEventPresenter : MonoBehaviour
{
    private const string OfficeSceneName = "YeniOfis";
    private const string FloorsSceneName = "katlar";

    private GameObject bannerRoot;
    private RectTransform banner;
    private RectTransform bannerShadow;
    private CanvasGroup canvasGroup;
    private Image accent;
    private Text title;
    private Text description;
    private Text effect;
    private Font displayFont;
    private Coroutine presentation;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSceneSubscription()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneInstaller()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (scene.name != OfficeSceneName &&
            scene.name != FloorsSceneName)
        {
            return;
        }

        GameObject presenterObject = new(
            "Daily Office Event Presenter",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        presenterObject.AddComponent<DailyOfficeEventPresenter>();
    }

    private void Awake()
    {
        ConfigureCanvas();
        LoadFont();
        BuildBanner();
    }

    private void OnEnable()
    {
        DailyOfficeEventSession.Changed += HandleEventChanged;
    }

    private void Start()
    {
        TryPresentPendingEvent();
    }

    private void OnDisable()
    {
        DailyOfficeEventSession.Changed -= HandleEventChanged;
    }

    private void HandleEventChanged()
    {
        if (DailyOfficeEventSession.CurrentType ==
            DailyOfficeEventType.None)
        {
            HideImmediately();
            return;
        }

        TryPresentPendingEvent();
    }

    private void TryPresentPendingEvent()
    {
        if (!DailyOfficeEventSession.TryConsumeAnnouncement(
                out DailyOfficeEventInfo info))
        {
            return;
        }

        if (presentation != null)
            StopCoroutine(presentation);
        presentation = StartCoroutine(Present(info));
    }

    private IEnumerator Present(DailyOfficeEventInfo info)
    {
        title.text = info.Title;
        description.text = info.Description;
        effect.text = info.EffectText;
        accent.color = info.AccentColor;
        canvasGroup.alpha = 1f;
        bannerRoot.SetActive(true);

        PlayEventSound(info.Type);
        StartCoroutine(MiniGameJuice.PopIn(
            bannerShadow,
            Vector3.one,
            0.32f,
            1.04f));
        yield return MiniGameJuice.PopIn(
            banner,
            Vector3.one,
            0.32f,
            1.08f);
        yield return new WaitForSecondsRealtime(3.3f);

        float elapsed = 0f;
        const float fadeDuration = 0.38f;
        while (elapsed < fadeDuration && canvasGroup != null)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(
                elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        bannerRoot.SetActive(false);
        presentation = null;
    }

    private void HideImmediately()
    {
        if (presentation != null)
        {
            StopCoroutine(presentation);
            presentation = null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (bannerRoot != null)
            bannerRoot.SetActive(false);
    }

    private static void PlayEventSound(DailyOfficeEventType type)
    {
        switch (type)
        {
            case DailyOfficeEventType.NetworkTrouble:
                ProceduralGameAudio.Play(GameSound.WrongAction, -0.08f);
                break;
            case DailyOfficeEventType.BossInspection:
                ProceduralGameAudio.Play(GameSound.BossWarning, 0.02f);
                break;
            case DailyOfficeEventType.CoffeeBoost:
                ProceduralGameAudio.Play(GameSound.TaskCompleted, 0.08f);
                break;
        }
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1050;

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

    private void BuildBanner()
    {
        bannerRoot = new GameObject(
            "EventBanner",
            typeof(RectTransform),
            typeof(CanvasGroup));
        bannerRoot.transform.SetParent(transform, false);
        canvasGroup = bannerRoot.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Image shadow = CreateImage(
            "Shadow",
            bannerRoot.transform,
            new Color(0f, 0.015f, 0.03f, 0.78f));
        SetRect(
            shadow.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(840f, 232f),
            new Vector2(10f, -255f),
            new Vector2(0.5f, 0.5f));
        bannerShadow = shadow.rectTransform;

        Image panel = CreateImage(
            "Panel",
            bannerRoot.transform,
            new Color(0.018f, 0.11f, 0.17f, 0.98f));
        banner = panel.rectTransform;
        SetRect(
            banner,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(820f, 220f),
            new Vector2(0f, -242f),
            new Vector2(0.5f, 0.5f));

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.5f, 0.68f, 0.8f);
        outline.effectDistance = new Vector2(3f, -3f);

        accent = CreateImage(
            "Accent",
            panel.transform,
            Color.white);
        SetRect(
            accent.rectTransform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(14f, 196f),
            new Vector2(11f, 0f),
            new Vector2(0.5f, 0.5f));

        title = CreateText(
            "Title",
            panel.transform,
            40,
            TextAnchor.MiddleLeft,
            new Color(1f, 0.9f, 0.42f, 1f));
        SetRect(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(700f, 62f),
            new Vector2(410f, -48f),
            new Vector2(0.5f, 0.5f));

        description = CreateText(
            "Description",
            panel.transform,
            26,
            TextAnchor.MiddleLeft,
            new Color(0.9f, 0.97f, 1f, 1f));
        SetRect(
            description.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(700f, 54f),
            new Vector2(410f, -108f),
            new Vector2(0.5f, 0.5f));

        effect = CreateText(
            "Effect",
            panel.transform,
            25,
            TextAnchor.MiddleLeft,
            new Color(0.42f, 0.9f, 1f, 1f));
        SetRect(
            effect.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(700f, 52f),
            new Vector2(410f, -169f),
            new Vector2(0.5f, 0.5f));

        bannerRoot.SetActive(false);
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
        outline.effectColor = new Color(0f, 0.02f, 0.05f, 0.95f);
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
}
