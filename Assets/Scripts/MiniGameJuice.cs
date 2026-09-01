using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Small, allocation-light presentation animations shared by the mini games.
/// Gameplay state remains owned by each mini-game controller.
/// </summary>
public static class MiniGameJuice
{
    public static IEnumerator PopIn(
        Transform target,
        Vector3 restingScale,
        float duration = 0.22f,
        float overshoot = 1.18f)
    {
        if (target == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        target.localScale = Vector3.zero;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float scale = EaseOutBack(progress, overshoot);
            target.localScale = restingScale * scale;
            yield return null;
        }

        if (target != null)
        {
            target.localScale = restingScale;
        }
    }

    public static IEnumerator PunchScale(
        Transform target,
        Vector3 restingScale,
        float strength = 0.18f,
        float duration = 0.2f)
    {
        if (target == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(progress * Mathf.PI) *
                (1f - progress) * strength;
            target.localScale = restingScale * (1f + pulse);
            yield return null;
        }

        if (target != null)
        {
            target.localScale = restingScale;
        }
    }

    public static IEnumerator ShakePosition(
        Transform target,
        Vector3 restingPosition,
        float strength = 0.08f,
        float duration = 0.2f,
        float frequency = 42f)
    {
        if (target == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float damping = 1f - progress;
            float horizontal = Mathf.Sin(elapsed * frequency) *
                strength * damping;
            float vertical = Mathf.Cos(elapsed * frequency * 0.73f) *
                strength * 0.35f * damping;
            target.position = restingPosition +
                new Vector3(horizontal, vertical, 0f);
            yield return null;
        }

        if (target != null)
        {
            target.position = restingPosition;
        }
    }

    public static IEnumerator FlashColor(
        SpriteRenderer renderer,
        Color flashColor,
        float duration = 0.24f,
        int flashCount = 2)
    {
        if (renderer == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        flashCount = Mathf.Max(1, flashCount);
        Color restingColor = renderer.color;
        flashColor.a = restingColor.a;
        float elapsed = 0f;

        while (elapsed < duration && renderer != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float blend = Mathf.Abs(
                Mathf.Sin(progress * Mathf.PI * flashCount));
            blend *= 1f - progress * 0.25f;
            renderer.color = Color.Lerp(restingColor, flashColor, blend);
            yield return null;
        }

        if (renderer != null)
        {
            renderer.color = restingColor;
        }
    }

    public static IEnumerator MoveScaleFade(
        SpriteRenderer renderer,
        Vector3 startPosition,
        Vector3 endPosition,
        Vector3 startScale,
        Vector3 endScale,
        float duration = 0.24f)
    {
        if (renderer == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        Color restingColor = renderer.color;
        float elapsed = 0f;

        while (elapsed < duration && renderer != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = SmoothStep01(elapsed / duration);
            renderer.transform.position =
                Vector3.LerpUnclamped(startPosition, endPosition, progress);
            renderer.transform.localScale =
                Vector3.LerpUnclamped(startScale, endScale, progress);

            Color color = restingColor;
            color.a = Mathf.Lerp(restingColor.a, 0f, progress);
            renderer.color = color;
            yield return null;
        }

        if (renderer != null)
        {
            renderer.transform.position = endPosition;
            renderer.transform.localScale = endScale;
            Color transparent = restingColor;
            transparent.a = 0f;
            renderer.color = transparent;
        }
    }

    public static IEnumerator FadeSprite(
        SpriteRenderer renderer,
        float fromAlpha,
        float toAlpha,
        float duration = 0.24f,
        bool disableWhenTransparent = false)
    {
        if (renderer == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        Color color = renderer.color;
        color.a = Mathf.Clamp01(fromAlpha);
        renderer.color = color;
        renderer.enabled = true;
        float elapsed = 0f;

        while (elapsed < duration && renderer != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = SmoothStep01(elapsed / duration);
            color.a = Mathf.Lerp(fromAlpha, toAlpha, progress);
            renderer.color = color;
            yield return null;
        }

        if (renderer != null)
        {
            color.a = Mathf.Clamp01(toAlpha);
            renderer.color = color;

            if (disableWhenTransparent && color.a <= 0.001f)
            {
                renderer.enabled = false;
            }
        }
    }

    public static IEnumerator MoveTransform(
        Transform target,
        Vector3 startPosition,
        Vector3 endPosition,
        float duration = 0.24f,
        float arcHeight = 0f)
    {
        if (target == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = SmoothStep01(elapsed / duration);
            Vector3 position = Vector3.LerpUnclamped(
                startPosition,
                endPosition,
                progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
            target.position = position;
            yield return null;
        }

        if (target != null)
        {
            target.position = endPosition;
        }
    }

    public static IEnumerator SquashSpinFadeOut(
        SpriteRenderer renderer,
        Vector3 startScale,
        float duration = 0.28f,
        float spinDegrees = 90f)
    {
        if (renderer == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        Transform target = renderer.transform;
        Quaternion startRotation = target.localRotation;
        Color startColor = renderer.color;
        float elapsed = 0f;

        while (elapsed < duration && renderer != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = SmoothStep01(progress);
            float width = Mathf.Lerp(1f, 1.35f, Mathf.Sin(progress * Mathf.PI));
            float height = Mathf.Lerp(1f, 0.05f, eased);
            target.localScale = Vector3.Scale(
                startScale,
                new Vector3(width, height, 1f));
            target.localRotation = startRotation *
                Quaternion.Euler(0f, 0f, spinDegrees * eased);

            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, eased);
            renderer.color = color;
            yield return null;
        }
    }

    public static IEnumerator IdleFloat(
        Transform target,
        Vector3 restingPosition,
        Vector3 restingScale,
        float phase,
        float height = 0.055f,
        float speed = 2.4f,
        float tiltDegrees = 3f)
    {
        while (target != null)
        {
            float wave = Mathf.Sin(Time.unscaledTime * speed + phase);
            float slowWave = Mathf.Sin(
                Time.unscaledTime * speed * 0.62f + phase * 1.7f);
            target.position = restingPosition +
                Vector3.up * wave * height;
            target.localScale = restingScale * (1f + slowWave * 0.025f);
            target.localRotation = Quaternion.Euler(
                0f,
                0f,
                slowWave * tiltDegrees);
            yield return null;
        }
    }

    private static float SmoothStep01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static float EaseOutBack(float value, float overshoot)
    {
        value = Mathf.Clamp01(value) - 1f;
        float amount = Mathf.Max(1f, overshoot) * 1.42f;
        return 1f + (amount + 1f) * value * value * value +
            amount * value * value;
    }
}

internal static class RuntimeOverlayFactory
{
    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        GameObject canvasObject = new(
            name,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static Font LoadDisplayFont()
    {
        GamePresentationLibrary library =
            Resources.Load<GamePresentationLibrary>(
                "GamePresentationLibrary");
        if (library != null && library.DisplayFont != null)
            return library.DisplayFont;

        return Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf");
    }

    public static Image CreateImage(
        string name,
        Transform parent,
        Color color)
    {
        GameObject imageObject = new(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static Text CreateText(
        string name,
        Transform parent,
        Font font,
        int size,
        TextAnchor alignment,
        Color color)
    {
        GameObject textObject = new(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.02f, 0.06f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
        return text;
    }

    public static void SetRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 size,
        Vector2 position,
        Vector2 pivot)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    public static void Stretch(
        RectTransform rect,
        Vector2 inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = inset;
        rect.offsetMax = -inset;
        rect.localScale = Vector3.one;
    }
}

internal static class MiniGamePresentationSession
{
    private static bool inputBlocked;

    public static bool IsInputBlocked => inputBlocked;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        inputBlocked = false;
    }

    public static void Begin()
    {
        inputBlocked = true;
    }

    public static void Complete()
    {
        inputBlocked = false;
    }

    public static void Cancel()
    {
        inputBlocked = false;
    }
}
internal sealed class MiniGamePresentationBootstrap : MonoBehaviour
{
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
        MiniGamePresentationSession.Cancel();
        SceneArrivalFade.Show();

        if (TryGetInstruction(
                scene.name,
                out string title,
                out string instruction))
        {
            MiniGamePresentationSession.Begin();
            MiniGameInstructionCard.Show(title, instruction);
        }
    }

    private static bool TryGetInstruction(
        string sceneName,
        out string title,
        out string instruction)
    {
        switch (sceneName)
        {
            case "kablo_game":
                title = "CABLE TROUBLE";
                instruction =
                    "Drag every coloured cable to its matching socket.";
                return true;
            case "Dosya_Y\u00FCkle":
                title = "UPLOAD QUEUE";
                instruction =
                    "Tap UPLOAD and let every file reach the folder.";
                return true;
            case "vir\u00FCs":
                title = "VIRUS ALERT";
                instruction =
                    "Tap all viruses before the system timer ends.";
                return true;
            case "bozukkasa":
                title = "EMERGENCY REPAIR";
                instruction =
                    "Hit the broken case five times to repair it.";
                return true;
            case "bozukmonit\u00F6r":
                title = "MONITOR MALFUNCTION";
                instruction =
                    "Five professional taps should fix the monitor.";
                return true;
            case "modem":
                title = "MODEM OFFLINE";
                instruction =
                    "Aim carefully and connect the modem cable.";
                return true;
            case "wifi_sinyal":
                title = "WEAK WI-FI";
                instruction =
                    "Move the device into the strongest signal zone.";
                return true;
            case "e_posta":
                title = "INBOX TRIAGE";
                instruction =
                    "Classify safe and suspicious emails correctly.";
                return true;
            case "kasa_par\u00E7a":
                title = "LOOSE COMPONENTS";
                instruction =
                    "Find and secure every damaged PC component.";
                return true;
            case "popup_ads":
                title = "POP-UP PANIC";
                instruction =
                    "Close bad ads, minimise good ones. Neutral is your call.";
                return true;
            case "pasword_game":
                title = "PASSWORD CHECK";
                instruction =
                    "Enter the eight-character password shown above.";
                return true;

            case "Server_Cooling":
                title = "SERVER OVERHEAT";
                instruction =
                    "Choose the right tool for each failing fan.";
                return true;
            case "Security_check":
                title = "SECURITY CHECK";
                instruction =
                    "Scan amber badges, then approve or reject every card.";
                return true;
            default:
                title = null;
                instruction = null;
                return false;
        }
    }
}

internal sealed class SceneArrivalFade : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    public static void Show()
    {
        Canvas canvas = RuntimeOverlayFactory.CreateCanvas(
            "Scene Arrival Fade",
            2000);
        SceneArrivalFade fade =
            canvas.gameObject.AddComponent<SceneArrivalFade>();
        Image curtain = RuntimeOverlayFactory.CreateImage(
            "Curtain",
            canvas.transform,
            Color.black);
        RuntimeOverlayFactory.Stretch(
            curtain.rectTransform,
            Vector2.zero);
        fade.canvasGroup =
            canvas.gameObject.AddComponent<CanvasGroup>();
        fade.canvasGroup.blocksRaycasts = false;
        fade.StartCoroutine(fade.FadeAway());
    }

    private IEnumerator FadeAway()
    {
        const float holdDuration = 0.04f;
        const float fadeDuration = 0.32f;
        float elapsed = 0f;

        while (elapsed < holdDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeDuration && canvasGroup != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = 1f -
                progress * progress * (3f - 2f * progress);
            yield return null;
        }

        Destroy(gameObject);
    }
}

internal sealed class MiniGameInstructionCard : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private RectTransform panel;
    private Text statusText;

    public static void Show(string title, string instruction)
    {
        Canvas canvas = RuntimeOverlayFactory.CreateCanvas(
            "Mini Game Instruction",
            1750);
        MiniGameInstructionCard card =
            canvas.gameObject.AddComponent<MiniGameInstructionCard>();
        card.Build(title, instruction);
    }

    private void Build(string title, string instruction)
    {
        Font font = RuntimeOverlayFactory.LoadDisplayFont();
        Image background = RuntimeOverlayFactory.CreateImage(
            "Instruction Card",
            transform,
            new Color(0.018f, 0.105f, 0.18f, 0.985f));
        background.raycastTarget = true;
        panel = background.rectTransform;
        RuntimeOverlayFactory.SetRect(
            panel,
            new Vector2(0.5f, 1f),
            new Vector2(900f, 250f),
            new Vector2(0f, -158f),
            new Vector2(0.5f, 1f));

        Outline frame = background.gameObject.AddComponent<Outline>();
        frame.effectColor = new Color(0.18f, 0.76f, 0.95f, 0.95f);
        frame.effectDistance = new Vector2(4f, -4f);

        Image accent = RuntimeOverlayFactory.CreateImage(
            "Accent",
            background.transform,
            new Color(0.22f, 0.9f, 1f, 1f));
        RuntimeOverlayFactory.SetRect(
            accent.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(900f, 9f),
            Vector2.zero,
            new Vector2(0.5f, 1f));

        Text titleText = RuntimeOverlayFactory.CreateText(
            "Title",
            background.transform,
            font,
            43,
            TextAnchor.MiddleCenter,
            new Color(0.32f, 0.94f, 1f, 1f));
        RuntimeOverlayFactory.SetRect(
            titleText.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(840f, 70f),
            new Vector2(0f, -28f),
            new Vector2(0.5f, 1f));
        titleText.text = title;

        Text instructionText = RuntimeOverlayFactory.CreateText(
            "Instruction",
            background.transform,
            font,
            27,
            TextAnchor.MiddleCenter,
            new Color(0.92f, 0.98f, 1f, 1f));
        RuntimeOverlayFactory.SetRect(
            instructionText.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(820f, 86f),
            new Vector2(0f, -96f),
            new Vector2(0.5f, 1f));
        instructionText.text = instruction;

        statusText = RuntimeOverlayFactory.CreateText(
            "Status",
            background.transform,
            font,
            32,
            TextAnchor.MiddleCenter,
            new Color(1f, 0.78f, 0.2f, 1f));
        RuntimeOverlayFactory.SetRect(
            statusText.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(420f, 62f),
            new Vector2(0f, 10f),
            new Vector2(0.5f, 0f));
        statusText.text = "READY";

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;
        StartCoroutine(ShowThenHide());
    }

    private IEnumerator ShowThenHide()
    {
        yield return MiniGameJuice.PopIn(
            panel,
            Vector3.one,
            0.24f,
            1.08f);
        yield return new WaitForSecondsRealtime(0.95f);

        if (statusText != null)
        {
            statusText.text = "GO!";
            statusText.color = new Color(0.35f, 1f, 0.58f, 1f);
            yield return MiniGameJuice.PunchScale(
                statusText.rectTransform,
                Vector3.one,
                0.25f,
                0.22f);
        }

        MiniGamePresentationSession.Complete();
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
        DeviceHaptics.Play(HapticFeedbackType.Selection);
        yield return new WaitForSecondsRealtime(0.28f);

        float elapsed = 0f;
        const float duration = 0.25f;
        while (elapsed < duration && canvasGroup != null)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        MiniGamePresentationSession.Complete();
    }
}
internal sealed class GameFeedbackOverlay : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private RectTransform panel;

    public static void ShowTaskCompleted(
        int awardedScore,
        bool bossCalmed,
        TaskQualityResult quality)
    {
        Color qualityColor = quality.Quality switch
        {
            TaskQuality.Perfect =>
                new Color(0.02f, 0.68f, 0.5f, 0.985f),
            TaskQuality.Good =>
                new Color(0.035f, 0.48f, 0.72f, 0.985f),
            _ => new Color(0.86f, 0.4f, 0.055f, 0.985f)
        };
        int responsePercent = Mathf.Clamp(
            Mathf.RoundToInt(quality.ResponseRatio * 100f),
            0,
            100);
        string mistakeLabel = quality.MistakeCount == 1
            ? "1 MISTAKE"
            : $"{quality.MistakeCount} MISTAKES";
        string details =
            $"+{awardedScore} PTS  |  {mistakeLabel}  |  {responsePercent}% TIME";
        if (bossCalmed)
            details += "\nBOSS CALMED";

        Show(quality.Label, details, qualityColor);
    }

    public static void ShowTaskFailed()
    {
        if (BossAngerSession.HasLost)
            return;

        Show(
            "TASK FAILED",
            "COMBO LOST  |  TRY THE NEXT ONE",
            new Color(0.8f, 0.1f, 0.1f, 0.985f));
    }

    private static void Show(
        string title,
        string subtitle,
        Color color)
    {
        Canvas canvas = RuntimeOverlayFactory.CreateCanvas(
            "Game Feedback",
            1600);
        DontDestroyOnLoad(canvas.gameObject);
        GameFeedbackOverlay feedback =
            canvas.gameObject.AddComponent<GameFeedbackOverlay>();
        feedback.Build(title, subtitle, color);
    }

    private void Build(
        string title,
        string subtitle,
        Color color)
    {
        Font font = RuntimeOverlayFactory.LoadDisplayFont();
        Image shadow = RuntimeOverlayFactory.CreateImage(
            "Feedback Shadow",
            transform,
            new Color(0f, 0.015f, 0.03f, 0.82f));
        RuntimeOverlayFactory.SetRect(
            shadow.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(842f, 226f),
            new Vector2(9f, -177f),
            new Vector2(0.5f, 1f));

        Image background = RuntimeOverlayFactory.CreateImage(
            "Feedback Panel",
            transform,
            color);
        panel = background.rectTransform;
        RuntimeOverlayFactory.SetRect(
            panel,
            new Vector2(0.5f, 1f),
            new Vector2(830f, 214f),
            new Vector2(0f, -168f),
            new Vector2(0.5f, 1f));

        Outline frame = background.gameObject.AddComponent<Outline>();
        frame.effectColor = new Color(0.88f, 1f, 1f, 0.92f);
        frame.effectDistance = new Vector2(4f, -4f);

        Text text = RuntimeOverlayFactory.CreateText(
            "Feedback Text",
            background.transform,
            font,
            45,
            TextAnchor.MiddleCenter,
            Color.white);
        RuntimeOverlayFactory.Stretch(
            text.rectTransform,
            new Vector2(28f, 15f));
        text.text = title + "\n<size=27>" + subtitle + "</size>";
        text.supportRichText = true;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 22;
        text.resizeTextMaxSize = 45;

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        StartCoroutine(ShowThenHide());
    }

    private IEnumerator ShowThenHide()
    {
        yield return MiniGameJuice.PopIn(
            panel,
            Vector3.one,
            0.24f,
            1.1f);
        yield return new WaitForSecondsRealtime(1.15f);

        float elapsed = 0f;
        const float duration = 0.3f;
        while (elapsed < duration && canvasGroup != null)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        Destroy(gameObject);
    }
}

internal sealed class WeeklyQualityReportOverlay : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private RectTransform panel;

    public static void Show(WeeklyQualityReport report)
    {
        Canvas canvas = RuntimeOverlayFactory.CreateCanvas(
            "Weekly Quality Report",
            1700);
        DontDestroyOnLoad(canvas.gameObject);
        WeeklyQualityReportOverlay overlay =
            canvas.gameObject.AddComponent<WeeklyQualityReportOverlay>();
        overlay.Build(report);
    }

    private void Build(WeeklyQualityReport report)
    {
        Font font = RuntimeOverlayFactory.LoadDisplayFont();
        Image background = RuntimeOverlayFactory.CreateImage(
            "Weekly Report Panel",
            transform,
            new Color(0.018f, 0.095f, 0.16f, 0.985f));
        panel = background.rectTransform;
        RuntimeOverlayFactory.SetRect(
            panel,
            new Vector2(0.5f, 0.5f),
            new Vector2(800f, 520f),
            Vector2.zero,
            new Vector2(0.5f, 0.5f));

        Outline frame = background.gameObject.AddComponent<Outline>();
        frame.effectColor = new Color(0.15f, 0.76f, 0.98f, 0.98f);
        frame.effectDistance = new Vector2(5f, -5f);

        Image header = RuntimeOverlayFactory.CreateImage(
            "Header",
            background.transform,
            new Color(0.035f, 0.3f, 0.47f, 1f));
        RuntimeOverlayFactory.SetRect(
            header.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(800f, 112f),
            Vector2.zero,
            new Vector2(0.5f, 1f));

        Text title = RuntimeOverlayFactory.CreateText(
            "Title",
            header.transform,
            font,
            43,
            TextAnchor.MiddleCenter,
            Color.white);
        RuntimeOverlayFactory.Stretch(title.rectTransform, new Vector2(18f, 8f));
        title.text = $"WEEK {report.Week:00} QUALITY REPORT";

        string verdict = GetVerdict(report);
        Text body = RuntimeOverlayFactory.CreateText(
            "Quality Totals",
            background.transform,
            font,
            32,
            TextAnchor.MiddleLeft,
            new Color(0.9f, 0.98f, 1f, 1f));
        RuntimeOverlayFactory.SetRect(
            body.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(650f, 300f),
            new Vector2(0f, -35f),
            new Vector2(0.5f, 0.5f));
        body.supportRichText = true;
        body.text =
            $"<color=#68FFC1>PERFECT</color>        {report.PerfectTasks:00}\n" +
            $"<color=#67CFFF>GOOD</color>           {report.GoodTasks:00}\n" +
            $"<color=#FFAD4D>MESSY</color>          {report.MessyTasks:00}\n" +
            $"<color=#FFFFFF>TOTAL</color>          {report.TotalTasks:00}\n\n" +
            $"<color=#FFE16A>{verdict}</color>";

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        DeviceHaptics.Play(HapticFeedbackType.Success);
        StartCoroutine(ShowThenHide());
    }

    private static string GetVerdict(WeeklyQualityReport report)
    {
        if (report.TotalTasks == 0)
            return "NO TASK DATA";
        if (report.PerfectTasks * 2 >= report.TotalTasks)
            return "OUTSTANDING WEEK";
        if (report.MessyTasks > report.PerfectTasks + report.GoodTasks)
            return "QUALITY NEEDS WORK";
        return "SOLID WEEK";
    }

    private IEnumerator ShowThenHide()
    {
        yield return MiniGameJuice.PopIn(
            panel,
            Vector3.one,
            0.26f,
            1.1f);
        yield return new WaitForSecondsRealtime(2.7f);

        float elapsed = 0f;
        const float duration = 0.35f;
        while (elapsed < duration && canvasGroup != null)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        Destroy(gameObject);
    }
}