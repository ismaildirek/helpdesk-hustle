using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class SurvivalTimeSession
{
    public const float DayDurationSeconds = 5f * 60f;
    public const float WeekDurationSeconds = 25f * 60f;
    public const int DaysPerWeek = 5;

    private static float elapsedSeconds;

    public static bool HasStarted { get; private set; }
    public static float ElapsedSeconds => elapsedSeconds;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        elapsedSeconds = 0f;
        HasStarted = false;
    }

    public static void BeginNewGame()
    {
        elapsedSeconds = 0f;
        HasStarted = true;
    }

    public static void EnsureStarted()
    {
        if (!HasStarted)
            BeginNewGame();
    }

    public static void Tick(float unscaledDeltaTime)
    {
        Tick(
            unscaledDeltaTime,
            BossIntroDialogue.IsBlockingOfficeInput);
    }

    internal static void Tick(
        float unscaledDeltaTime,
        bool introIsBlocking)
    {
        if (!HasStarted ||
            BossAngerSession.HasLost ||
            GamePauseSession.IsPaused ||
            introIsBlocking)
            return;

        elapsedSeconds += Mathf.Max(0f, unscaledDeltaTime);
    }
}

[DisallowMultipleComponent]
public sealed class SurvivalTimeDisplay : MonoBehaviour
{
    private const string OfficeSceneName = "YeniOfis";
    private const string DisplayObjectName = "zaman_g\u00F6sterge";

    private TextMesh timeValue;
    private TextMesh dayValue;
    private TextMesh weekValue;
    private SpriteRenderer backgroundRenderer;
    private int lastDisplayedSecond = -1;
    private int lastDisplayedDay = -1;
    private int lastDisplayedWeek = -1;

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
        if (!string.Equals(
                scene.name,
                OfficeSceneName,
                System.StringComparison.Ordinal))
        {
            return;
        }

        GameObject displayObject = GameObject.Find(DisplayObjectName);
        if (displayObject == null)
        {
            Debug.LogWarning(
                $"{DisplayObjectName} was not found in {OfficeSceneName}.");
            return;
        }

        if (displayObject.GetComponent<SurvivalTimeDisplay>() == null)
            displayObject.AddComponent<SurvivalTimeDisplay>();
    }

    private void Awake()
    {
        SurvivalTimeSession.EnsureStarted();

        GamePresentationLibrary library =
            Resources.Load<GamePresentationLibrary>(
                "GamePresentationLibrary");
        Font displayFont = library != null
            ? library.DisplayFont
            : null;

        if (displayFont == null)
        {
            Debug.LogError(
                "Survival time display could not find its font.",
                this);
            enabled = false;
            return;
        }

        backgroundRenderer = GetComponent<SpriteRenderer>();

        int sortingOrder = backgroundRenderer != null
            ? backgroundRenderer.sortingOrder + 1
            : 51;

        timeValue = CreateValueText(
            "TimeValue",
            displayFont,
            new Vector3(12.45f, 5.55f, -0.2f),
            0.62f,
            sortingOrder);
        dayValue = CreateValueText(
            "DayValue",
            displayFont,
            new Vector3(12.45f, 0f, -0.2f),
            0.7f,
            sortingOrder);
        weekValue = CreateValueText(
            "WeekValue",
            displayFont,
            new Vector3(12.45f, -5.55f, -0.2f),
            0.7f,
            sortingOrder);

        RefreshValues(true);
    }

    private void Update()
    {
        RefreshValues(false);
    }

    private void RefreshValues(bool force)
    {
        int totalSeconds = Mathf.Max(
            0,
            Mathf.FloorToInt(SurvivalTimeSession.ElapsedSeconds));
        if (!force && totalSeconds == lastDisplayedSecond)
            return;

        lastDisplayedSecond = totalSeconds;
        int secondsIntoDay = Mathf.FloorToInt(
            Mathf.Repeat(
                totalSeconds,
                SurvivalTimeSession.DayDurationSeconds));
        int minutes = secondsIntoDay / 60;
        int seconds = secondsIntoDay % 60;
        int totalDays = totalSeconds /
            Mathf.RoundToInt(SurvivalTimeSession.DayDurationSeconds);
        int dayInWeek = totalDays % SurvivalTimeSession.DaysPerWeek + 1;
        int week = totalSeconds /
            Mathf.RoundToInt(SurvivalTimeSession.WeekDurationSeconds) + 1;

        bool dayChanged = lastDisplayedDay >= 0 &&
            dayInWeek != lastDisplayedDay;
        bool weekChanged = lastDisplayedWeek >= 0 &&
            week != lastDisplayedWeek;
        lastDisplayedDay = dayInWeek;
        lastDisplayedWeek = week;

        timeValue.text = $"{minutes:00}:{seconds:00}";
        dayValue.text = $"{dayInWeek:00}";
        weekValue.text = $"{week:00}";

        if (dayChanged)
        {
            StartCoroutine(AnimatePeriodValue(
                dayValue,
                new Color32(255, 210, 72, 255)));
        }

        if (weekChanged)
        {
            StartCoroutine(AnimatePeriodValue(
                weekValue,
                new Color32(86, 255, 150, 255)));
        }

        if ((dayChanged || weekChanged) && backgroundRenderer != null)
        {
            StartCoroutine(MiniGameJuice.FlashColor(
                backgroundRenderer,
                weekChanged
                    ? new Color(0.28f, 1f, 0.58f)
                    : new Color(1f, 0.76f, 0.22f),
                0.55f,
                2));
        }
    }

    private static IEnumerator AnimatePeriodValue(
        TextMesh value,
        Color highlightColor)
    {
        if (value == null)
            yield break;

        Transform target = value.transform;
        Vector3 restingScale = target.localScale;
        Color restingColor = value.color;
        highlightColor.a = restingColor.a;
        const float duration = 0.58f;
        float elapsed = 0f;

        while (elapsed < duration && value != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(progress * Mathf.PI);
            target.localScale = restingScale * (1f + pulse * 0.32f);
            value.color = Color.Lerp(
                restingColor,
                highlightColor,
                pulse);
            yield return null;
        }

        if (value != null)
        {
            target.localScale = restingScale;
            value.color = restingColor;
        }
    }

    private TextMesh CreateValueText(
        string objectName,
        Font font,
        Vector3 localPosition,
        float characterSize,
        int sortingOrder)
    {
        GameObject valueObject = new(objectName);
        valueObject.transform.SetParent(transform, false);
        valueObject.transform.localPosition = localPosition;
        valueObject.transform.localRotation = Quaternion.identity;
        valueObject.transform.localScale = Vector3.one;

        TextMesh text = valueObject.AddComponent<TextMesh>();
        text.font = font;
        text.fontSize = 64;
        text.characterSize = characterSize;
        text.anchor = TextAnchor.MiddleRight;
        text.alignment = TextAlignment.Right;
        text.fontStyle = FontStyle.Bold;
        text.richText = false;
        text.color = new Color32(255, 244, 190, 255);

        MeshRenderer renderer = valueObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.sortingOrder = sortingOrder;
        return text;
    }
}

internal sealed class SurvivalTimeClock : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameObject clockObject = new("Survival Time Clock");
        DontDestroyOnLoad(clockObject);
        clockObject.AddComponent<SurvivalTimeClock>();
    }

    private void Update()
    {
        SurvivalTimeSession.Tick(Time.unscaledDeltaTime);
        DailyOfficeEventSession.Tick(
            SurvivalTimeSession.ElapsedSeconds);

        if (GameProgressionSession.TryCreateWeeklyQualityReport(
                out WeeklyQualityReport report))
        {
            WeeklyQualityReportOverlay.Show(report);
        }
    }
}

public readonly struct GameRunSummary
{
    public GameRunSummary(
        int score,
        int bestScore,
        float elapsedSeconds,
        float bestTime,
        int week,
        int day,
        int completedTasks,
        int failedTasks,
        int highestCombo,
        bool isNewBestScore,
        bool isNewBestTime)
    {
        Score = score;
        BestScore = bestScore;
        ElapsedSeconds = elapsedSeconds;
        BestTime = bestTime;
        Week = week;
        Day = day;
        CompletedTasks = completedTasks;
        FailedTasks = failedTasks;
        HighestCombo = highestCombo;
        IsNewBestScore = isNewBestScore;
        IsNewBestTime = isNewBestTime;
    }

    public int Score { get; }
    public int BestScore { get; }
    public float ElapsedSeconds { get; }
    public float BestTime { get; }
    public int Week { get; }
    public int Day { get; }
    public int CompletedTasks { get; }
    public int FailedTasks { get; }
    public int HighestCombo { get; }
    public bool IsNewBestScore { get; }
    public bool IsNewBestTime { get; }
}

public readonly struct WeeklyQualityReport
{
    public WeeklyQualityReport(
        int week,
        int perfectTasks,
        int goodTasks,
        int messyTasks)
    {
        Week = Mathf.Max(1, week);
        PerfectTasks = Mathf.Max(0, perfectTasks);
        GoodTasks = Mathf.Max(0, goodTasks);
        MessyTasks = Mathf.Max(0, messyTasks);
    }

    public int Week { get; }
    public int PerfectTasks { get; }
    public int GoodTasks { get; }
    public int MessyTasks { get; }
    public int TotalTasks => PerfectTasks + GoodTasks + MessyTasks;
}
public static class GameProgressionSession
{
    private const string BestScoreKey = "OfficeGame.BestScore";
    private const string BestTimeKey = "OfficeGame.BestTime";
    private const string BestTasksKey = "OfficeGame.BestTasks";
    private const int BaseTaskScore = 100;
    private const int ScorePerRemainingSecond = 3;
    private const float ComboTierBonus = 0.15f;
    private const float WeeklyTaskTimeMultiplier = 0.93f;
    private const float MinimumTaskTimeMultiplier = 0.58f;

    private static int score;
    private static int combo;
    private static int highestCombo;
    private static int completedTasks;
    private static int failedTasks;
    private static int lastAwardedScore;
    private static int bestScore;
    private static int bestCompletedTasks;
    private static float bestTime;
    private static bool runActive;
    private static bool hasPendingSummary;
    private static GameRunSummary pendingSummary;
    private static int trackedQualityWeek = 1;
    private static int weeklyPerfectTasks;
    private static int weeklyGoodTasks;
    private static int weeklyMessyTasks;

    public static event Action Changed;

    public static int Score => score;
    public static int Combo => combo;
    public static int HighestCombo => highestCombo;
    public static int CompletedTasks => completedTasks;
    public static int FailedTasks => failedTasks;
    public static int LastAwardedScore => lastAwardedScore;
    public static int BestScore => bestScore;
    public static int BestCompletedTasks => bestCompletedTasks;
    public static float BestTime => bestTime;
    public static bool HasPendingSummary => hasPendingSummary;
    public static int CurrentWeek => Mathf.Max(
        1,
        Mathf.FloorToInt(
            SurvivalTimeSession.ElapsedSeconds /
            SurvivalTimeSession.WeekDurationSeconds) + 1);
    public static int CurrentDay => Mathf.FloorToInt(
        SurvivalTimeSession.ElapsedSeconds /
        SurvivalTimeSession.DayDurationSeconds) %
        SurvivalTimeSession.DaysPerWeek + 1;
    public static int DifficultyLevel => Mathf.Clamp(
        CurrentWeek - 1,
        0,
        8);
    public static float TaskDurationMultiplier => Mathf.Max(
        MinimumTaskTimeMultiplier,
        Mathf.Pow(
            WeeklyTaskTimeMultiplier,
            CurrentWeek - 1)) *
        DailyOfficeEventSession.TaskDurationMultiplier;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        score = 0;
        combo = 0;
        highestCombo = 0;
        completedTasks = 0;
        failedTasks = 0;
        lastAwardedScore = 0;
        runActive = false;
        hasPendingSummary = false;
        pendingSummary = default;
        trackedQualityWeek = 1;
        weeklyPerfectTasks = 0;
        weeklyGoodTasks = 0;
        weeklyMessyTasks = 0;
        LoadPersistentRecords();
        Changed = null;
    }

    public static void BeginNewGame()
    {
        score = 0;
        combo = 0;
        highestCombo = 0;
        completedTasks = 0;
        failedTasks = 0;
        lastAwardedScore = 0;
        runActive = true;
        hasPendingSummary = false;
        pendingSummary = default;
        trackedQualityWeek = Mathf.Max(1, CurrentWeek);
        weeklyPerfectTasks = 0;
        weeklyGoodTasks = 0;
        weeklyMessyTasks = 0;
        LoadPersistentRecords();
        Changed?.Invoke();
    }

    public static void EnsureRunStarted()
    {
        if (!runActive && !hasPendingSummary)
            BeginNewGame();
    }

    public static int RegisterTaskCompleted(float remainingTime)
    {
        return RegisterTaskCompleted(remainingTime, 1f);
    }

    internal static int RegisterTaskCompleted(
        float remainingTime,
        TaskQualityResult quality)
    {
        EnsureRunStarted();
        if (!runActive)
            return 0;

        switch (quality.Quality)
        {
            case TaskQuality.Perfect:
                weeklyPerfectTasks++;
                break;
            case TaskQuality.Good:
                weeklyGoodTasks++;
                break;
            default:
                weeklyMessyTasks++;
                break;
        }

        return RegisterTaskCompleted(
            remainingTime,
            quality.ScoreMultiplier);
    }
    public static int RegisterTaskCompleted(
        float remainingTime,
        float performanceMultiplier)
    {
        EnsureRunStarted();
        if (!runActive)
            return 0;

        completedTasks++;
        combo++;
        highestCombo = Mathf.Max(highestCombo, combo);

        int timeBonus = Mathf.Max(
            0,
            Mathf.CeilToInt(remainingTime) *
            ScorePerRemainingSecond);
        int comboTier = Mathf.Max(0, combo / 3);
        float comboMultiplier = 1f + comboTier * ComboTierBonus;
        int standardAward = Mathf.Max(
            BaseTaskScore,
            Mathf.RoundToInt(
                (BaseTaskScore + timeBonus) *
                comboMultiplier *
                DailyOfficeEventSession.ScoreMultiplier));
        lastAwardedScore = Mathf.Max(
            1,
            Mathf.RoundToInt(
                standardAward * Mathf.Max(0f, performanceMultiplier)));
        score += lastAwardedScore;
        Changed?.Invoke();
        return lastAwardedScore;
    }
    public static void RegisterTaskFailed()
    {
        EnsureRunStarted();
        if (!runActive)
            return;

        failedTasks++;
        combo = 0;
        lastAwardedScore = 0;
        Changed?.Invoke();
    }

    public static bool TryCreateWeeklyQualityReport(
        out WeeklyQualityReport report)
    {
        int currentWeek = Mathf.Max(1, CurrentWeek);
        if (currentWeek <= trackedQualityWeek)
        {
            report = default;
            return false;
        }

        report = new WeeklyQualityReport(
            trackedQualityWeek,
            weeklyPerfectTasks,
            weeklyGoodTasks,
            weeklyMessyTasks);
        trackedQualityWeek = currentWeek;
        weeklyPerfectTasks = 0;
        weeklyGoodTasks = 0;
        weeklyMessyTasks = 0;
        return true;
    }
    public static void FinalizeRun()
    {
        if (!runActive || hasPendingSummary)
            return;

        runActive = false;
        float elapsed = SurvivalTimeSession.ElapsedSeconds;
        bool newBestScore = score > bestScore;
        bool newBestTime = elapsed > bestTime;

        if (newBestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
        }

        if (newBestTime)
        {
            bestTime = elapsed;
            PlayerPrefs.SetFloat(BestTimeKey, bestTime);
        }

        if (completedTasks > bestCompletedTasks)
        {
            bestCompletedTasks = completedTasks;
            PlayerPrefs.SetInt(BestTasksKey, bestCompletedTasks);
        }

        PlayerPrefs.Save();
        pendingSummary = new GameRunSummary(
            score,
            bestScore,
            elapsed,
            bestTime,
            CurrentWeek,
            CurrentDay,
            completedTasks,
            failedTasks,
            highestCombo,
            newBestScore,
            newBestTime);
        hasPendingSummary = true;
        Changed?.Invoke();
    }

    public static bool TryGetPendingSummary(out GameRunSummary summary)
    {
        summary = pendingSummary;
        return hasPendingSummary;
    }

    public static void DismissPendingSummary()
    {
        hasPendingSummary = false;
        pendingSummary = default;
        Changed?.Invoke();
    }

    private static void LoadPersistentRecords()
    {
        bestScore = Mathf.Max(0, PlayerPrefs.GetInt(BestScoreKey, 0));
        bestCompletedTasks = Mathf.Max(
            0,
            PlayerPrefs.GetInt(BestTasksKey, 0));
        bestTime = Mathf.Max(0f, PlayerPrefs.GetFloat(BestTimeKey, 0f));
    }
}

internal sealed class GameProgressionUI : MonoBehaviour
{
    private const string OfficeSceneName = "YeniOfis";
    private const string EntranceSceneName = "Giris_Ekran";

    private enum DisplayMode
    {
        OfficeHud,
        RunSummary
    }

    private DisplayMode mode;
    private Font displayFont;
    private Text hudText;
    private RectTransform resultPanel;
    private RectTransform retryButton;
    private RectTransform menuButton;
    private EntrancePlayButton entrancePlayButton;
    private bool actionRequested;

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
        LoadSceneMode loadMode)
    {
        if (scene.name == OfficeSceneName)
        {
            Install(DisplayMode.OfficeHud);
            return;
        }

        if (scene.name == EntranceSceneName &&
            GameProgressionSession.HasPendingSummary)
        {
            Install(DisplayMode.RunSummary);
        }
    }

    private static void Install(DisplayMode displayMode)
    {
        GameObject uiObject = new(
            displayMode == DisplayMode.OfficeHud
                ? "Game Progression HUD"
                : "Run Result Screen",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        GameProgressionUI ui = uiObject.AddComponent<GameProgressionUI>();
        ui.Configure(displayMode);
    }

    private void OnEnable()
    {
        GameProgressionSession.Changed += RefreshHud;
    }

    private void OnDisable()
    {
        GameProgressionSession.Changed -= RefreshHud;
    }

    private void Configure(DisplayMode displayMode)
    {
        mode = displayMode;
        ConfigureCanvas();
        LoadFont();

        if (mode == DisplayMode.OfficeHud)
        {
            GameProgressionSession.EnsureRunStarted();
            BuildOfficeHud();
            RefreshHud();
        }
        else
        {
            BuildRunSummary();
        }
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

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

    private void BuildOfficeHud()
    {
        Image panel = CreateImage(
            "ProgressionPanel",
            transform,
            new Color(0.018f, 0.075f, 0.12f, 0.88f));
        SetRect(
            panel.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(660f, 84f),
            new Vector2(0f, -28f),
            new Vector2(0.5f, 1f));

        Outline panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.08f, 0.48f, 0.68f, 0.65f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        hudText = CreateText(
            "ProgressionText",
            panel.transform,
            25,
            TextAnchor.MiddleCenter,
            new Color(0.92f, 0.98f, 1f, 1f));
        Stretch(hudText.rectTransform, new Vector2(14f, 7f));
    }

    private void BuildRunSummary()
    {
        if (!GameProgressionSession.TryGetPendingSummary(
                out GameRunSummary summary))
        {
            Destroy(gameObject);
            return;
        }

        Image dimmer = CreateImage(
            "Dimmer",
            transform,
            new Color(0.005f, 0.015f, 0.03f, 0.86f));
        Stretch(dimmer.rectTransform, Vector2.zero);

        Image shadow = CreateImage(
            "ResultShadow",
            dimmer.transform,
            new Color(0f, 0.01f, 0.02f, 0.78f));
        SetRect(
            shadow.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(860f, 1220f),
            new Vector2(12f, -16f),
            new Vector2(0.5f, 0.5f));

        Image frame = CreateImage(
            "ResultFrame",
            dimmer.transform,
            new Color(0.035f, 0.42f, 0.58f, 0.98f));
        resultPanel = frame.rectTransform;
        SetRect(
            resultPanel,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(840f, 1200f),
            Vector2.zero,
            new Vector2(0.5f, 0.5f));

        Image panel = CreateImage(
            "ResultPanel",
            frame.transform,
            new Color(0.018f, 0.11f, 0.18f, 1f));
        Stretch(panel.rectTransform, new Vector2(12f, 12f));

        Image header = CreateImage(
            "HeaderBand",
            panel.transform,
            new Color(0.025f, 0.2f, 0.3f, 1f));
        SetRect(
            header.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(790f, 178f),
            new Vector2(0f, -89f),
            new Vector2(0.5f, 0.5f));

        Image headerAccent = CreateImage(
            "HeaderAccent",
            panel.transform,
            new Color(1f, 0.3f, 0.18f, 1f));
        SetRect(
            headerAccent.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(790f, 9f),
            new Vector2(0f, -5f),
            new Vector2(0.5f, 0.5f));

        Text title = CreateText(
            "Title",
            panel.transform,
            64,
            TextAnchor.MiddleCenter,
            new Color(1f, 0.33f, 0.22f, 1f));
        SetRect(
            title.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(720f, 104f),
            new Vector2(0f, -66f),
            new Vector2(0.5f, 0.5f));
        title.text = "SHIFT OVER";

        Text subtitle = CreateText(
            "Subtitle",
            panel.transform,
            25,
            TextAnchor.MiddleCenter,
            new Color(0.44f, 0.84f, 1f, 1f));
        SetRect(
            subtitle.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(720f, 48f),
            new Vector2(0f, -142f),
            new Vector2(0.5f, 0.5f));
        subtitle.text = "OFFICE PERFORMANCE REPORT";

        Image detailsPanel = CreateImage(
            "StatisticsPanel",
            panel.transform,
            new Color(0.01f, 0.065f, 0.11f, 0.92f));
        SetRect(
            detailsPanel.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(700f, 560f),
            new Vector2(0f, -486f),
            new Vector2(0.5f, 0.5f));

        Outline detailsOutline =
            detailsPanel.gameObject.AddComponent<Outline>();
        detailsOutline.effectColor =
            new Color(0.08f, 0.36f, 0.5f, 0.72f);
        detailsOutline.effectDistance = new Vector2(2f, -2f);

        Text details = CreateText(
            "Details",
            detailsPanel.transform,
            37,
            TextAnchor.UpperCenter,
            new Color(0.92f, 0.98f, 1f, 1f));
        Stretch(details.rectTransform, new Vector2(34f, 28f));
        details.lineSpacing = 1.18f;
        details.text = BuildSummaryText(summary);

        if (summary.IsNewBestScore || summary.IsNewBestTime)
        {
            Image recordBand = CreateImage(
                "NewRecordBand",
                panel.transform,
                new Color(0.35f, 0.22f, 0.025f, 0.96f));
            SetRect(
                recordBand.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(610f, 76f),
                new Vector2(0f, -217f),
                new Vector2(0.5f, 0.5f));

            Text record = CreateText(
                "NewRecord",
                recordBand.transform,
                34,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.24f, 1f));
            Stretch(record.rectTransform, new Vector2(12f, 7f));
            record.text = "NEW OFFICE RECORD!";
        }

        retryButton = CreateActionButton(
            panel.transform,
            "RETRY",
            new Vector2(0f, -356f),
            new Color(0.08f, 0.68f, 0.42f, 1f));
        menuButton = CreateActionButton(
            panel.transform,
            "MAIN MENU",
            new Vector2(0f, -492f),
            new Color(0.12f, 0.42f, 0.72f, 1f));

        entrancePlayButton = FindFirstObjectByType<EntrancePlayButton>(
            FindObjectsInactive.Include);
        if (entrancePlayButton != null)
            entrancePlayButton.enabled = false;

        StartCoroutine(MiniGameJuice.PopIn(
            shadow.rectTransform,
            Vector3.one,
            0.32f,
            1.05f));
        StartCoroutine(MiniGameJuice.PopIn(
            resultPanel,
            Vector3.one,
            0.32f,
            1.08f));
    }

    private void Update()
    {
        if (mode != DisplayMode.RunSummary ||
            actionRequested ||
            !TryReadPointerPress(out Vector2 screenPosition))
        {
            return;
        }

        if (retryButton != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                retryButton,
                screenPosition,
                null))
        {
            actionRequested = true;
            ProceduralGameAudio.Play(GameSound.UiClick, 0.03f);
            BossIntroDialogue.BeginNewGame();
            SceneManager.LoadScene(OfficeSceneName);
            return;
        }

        if (menuButton != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                menuButton,
                screenPosition,
                null))
        {
            actionRequested = true;
            ProceduralGameAudio.Play(GameSound.UiClick, -0.02f);
            GameProgressionSession.DismissPendingSummary();
            if (entrancePlayButton != null)
                entrancePlayButton.enabled = true;
            Destroy(gameObject);
        }
    }

    private void RefreshHud()
    {
        if (hudText == null)
            return;

        string comboText = GameProgressionSession.Combo > 0
            ? $"x{GameProgressionSession.Combo:00}"
            : "--";
        hudText.text =
            $"SCORE {GameProgressionSession.Score:000000}    " +
            $"COMBO {comboText}    " +
            $"TASKS {GameProgressionSession.CompletedTasks:00}";
    }

    private string BuildSummaryText(GameRunSummary summary)
    {
        return
            $"<color=#5EDCFF>SCORE</color>         " +
            $"<color=#FFFFFF>{summary.Score:000000}</color>\n" +
            $"<color=#5EDCFF>BEST SCORE</color>    " +
            $"<color=#FFE05C>{summary.BestScore:000000}</color>\n\n" +
            $"<color=#5EDCFF>SURVIVED</color>      " +
            $"<color=#FFFFFF>{FormatTime(summary.ElapsedSeconds)}</color>\n" +
            $"<color=#5EDCFF>BEST TIME</color>     " +
            $"<color=#FFE05C>{FormatTime(summary.BestTime)}</color>\n\n" +
            $"<color=#8DE6FF>WEEK {summary.Week:00}   " +
            $"DAY {summary.Day:00}</color>\n" +
            $"COMPLETED     <color=#63F2A7>" +
            $"{summary.CompletedTasks:00}</color>\n" +
            $"MISSED        <color=#FF6C5F>" +
            $"{summary.FailedTasks:00}</color>\n" +
            $"BEST COMBO    <color=#FFE05C>" +
            $"x{summary.HighestCombo:00}</color>";
    }

    private RectTransform CreateActionButton(
        Transform parent,
        string label,
        Vector2 position,
        Color color)
    {
        Image image = CreateImage(label, parent, color);
        SetRect(
            image.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(570f, 112f),
            position,
            new Vector2(0.5f, 0.5f));

        Shadow shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0.02f, 0.04f, 0.82f);
        shadow.effectDistance = new Vector2(0f, -7f);

        Outline outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.9f, 1f, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);

        Text text = CreateText(
            $"{label} Label",
            image.transform,
            42,
            TextAnchor.MiddleCenter,
            Color.white);
        Stretch(text.rectTransform, new Vector2(12f, 8f));
        text.text = label;
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
        outline.effectColor = new Color(0f, 0.03f, 0.08f, 0.92f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
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

    private static void Stretch(
        RectTransform rect,
        Vector2 inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = inset;
        rect.offsetMax = -inset;
        rect.localScale = Vector3.one;
    }

    private static string FormatTime(float totalSeconds)
    {
        int safeSeconds = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
        int hours = safeSeconds / 3600;
        int minutes = safeSeconds / 60 % 60;
        int seconds = safeSeconds % 60;
        return hours > 0
            ? $"{hours:00}:{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";
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
