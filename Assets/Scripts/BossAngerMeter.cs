using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossAngerMeter : MonoBehaviour
{
    private static readonly Rect FrameTrackRect =
        new(0.195f, 0.44f, 0.61f, 0.15f);
    private static readonly Rect GreenArtworkRect =
        new(0.27f, 0.43f, 0.46f, 0.21f);
    private static readonly Rect YellowArtworkRect =
        new(0.32f, 0.42f, 0.36f, 0.20f);
    private static readonly Rect RedArtworkRect =
        new(0.25f, 0.45f, 0.50f, 0.18f);

    private const float GreenContentLeft = 0.27f;
    private const float GreenContentRight = 0.73f;
    private const float YellowContentLeft = 0.32f;
    private const float YellowContentRight = 0.68f;
    private const float RedContentLeft = 0.25f;
    private const float RedContentRight = 0.75f;

    [SerializeField] private SpriteRenderer greenBar;
    [SerializeField] private SpriteRenderer yellowBar;
    [SerializeField] private SpriteRenderer redBar;

    private GameObject fillMaskObject;
    private Texture2D fillMaskTexture;
    private Sprite fillMaskSprite;
    private SpriteMask fillMask;
    private bool sortingOrdersCaptured;
    private int frameSortingOrder;
    private int greenSortingOrder;
    private int yellowSortingOrder;
    private int redSortingOrder;
    private int lastFailureCount;
    private Vector3 frameRestPosition;
    private Vector3 greenRestPosition;
    private Vector3 yellowRestPosition;
    private Vector3 redRestPosition;
    private Color frameRestColor;
    private Color greenRestColor;
    private Color yellowRestColor;
    private Color redRestColor;
    private Coroutine angerFeedbackRoutine;

    private void OnEnable()
    {
        CaptureSortingOrders();
        AlignBarsToFrameTrack();
        EnsureFillMask();
        CaptureFeedbackState();
        lastFailureCount = BossAngerSession.FailureCount;
        BossAngerSession.Changed += HandleAngerChanged;
        Refresh();
    }

    private void OnDisable()
    {
        BossAngerSession.Changed -= HandleAngerChanged;
        RestoreFeedbackState();
    }

    private void OnDestroy()
    {
        if (fillMaskSprite != null)
            Destroy(fillMaskSprite);

        if (fillMaskTexture != null)
            Destroy(fillMaskTexture);
    }

    private void Refresh()
    {
        int stage = BossAngerSession.VisualStage;
        SetVisible(greenBar, false);
        SetVisible(yellowBar, false);
        SetVisible(redBar, false);

        if (fillMask == null || stage < 0)
        {
            SetMaskVisible(false);
            return;
        }

        SpriteRenderer activeBar;
        float contentLeft;
        float contentRight;

        switch (stage)
        {
            case 0:
                activeBar = greenBar;
                contentLeft = GreenContentLeft;
                contentRight = GreenContentRight;
                break;
            case 1:
                activeBar = yellowBar;
                contentLeft = YellowContentLeft;
                contentRight = YellowContentRight;
                break;
            default:
                activeBar = redBar;
                contentLeft = RedContentLeft;
                contentRight = RedContentRight;
                break;
        }

        if (activeBar == null || activeBar.sprite == null)
        {
            SetMaskVisible(false);
            return;
        }

        SetVisible(activeBar, true);
        UpdateFillMask(
            activeBar,
            BossAngerSession.FillAmount,
            contentLeft,
            contentRight);
    }

    private void HandleAngerChanged()
    {
        int failureCount = BossAngerSession.FailureCount;
        bool increased = failureCount > lastFailureCount;
        lastFailureCount = failureCount;
        Refresh();

        if (!increased)
            return;

        if (angerFeedbackRoutine != null)
        {
            StopCoroutine(angerFeedbackRoutine);
            RestoreFeedbackState();
        }

        ProceduralGameAudio.Play(GameSound.BossWarning, 0.025f);
        angerFeedbackRoutine = StartCoroutine(AnimateAngerIncrease());
    }

    private IEnumerator AnimateAngerIncrease()
    {
        const float duration = 0.48f;
        const float shakeStrength = 0.055f;
        float elapsed = 0f;
        SpriteRenderer frameRenderer = GetComponent<SpriteRenderer>();

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float damping = 1f - progress;
            Vector3 offset = new(
                Mathf.Sin(elapsed * 72f) * shakeStrength * damping,
                Mathf.Cos(elapsed * 53f) * shakeStrength * 0.35f * damping,
                0f);
            float flash = Mathf.Abs(Mathf.Sin(progress * Mathf.PI * 4f));
            flash *= damping;

            ApplyFeedbackOffset(offset);
            ApplyFeedbackColor(
                frameRenderer,
                frameRestColor,
                flash);
            ApplyFeedbackColor(greenBar, greenRestColor, flash);
            ApplyFeedbackColor(yellowBar, yellowRestColor, flash);
            ApplyFeedbackColor(redBar, redRestColor, flash);
            yield return null;
        }

        RestoreFeedbackState();
        angerFeedbackRoutine = null;
    }

    private void CaptureFeedbackState()
    {
        SpriteRenderer frameRenderer = GetComponent<SpriteRenderer>();
        frameRestPosition = transform.position;
        greenRestPosition = GetPosition(greenBar);
        yellowRestPosition = GetPosition(yellowBar);
        redRestPosition = GetPosition(redBar);
        frameRestColor = GetColor(frameRenderer);
        greenRestColor = GetColor(greenBar);
        yellowRestColor = GetColor(yellowBar);
        redRestColor = GetColor(redBar);
    }

    private void ApplyFeedbackOffset(Vector3 offset)
    {
        transform.position = frameRestPosition + offset;
        SetPosition(greenBar, greenRestPosition + offset);
        SetPosition(yellowBar, yellowRestPosition + offset);
        SetPosition(redBar, redRestPosition + offset);
    }

    private void RestoreFeedbackState()
    {
        SpriteRenderer frameRenderer = GetComponent<SpriteRenderer>();
        transform.position = frameRestPosition;
        SetPosition(greenBar, greenRestPosition);
        SetPosition(yellowBar, yellowRestPosition);
        SetPosition(redBar, redRestPosition);
        SetColor(frameRenderer, frameRestColor);
        SetColor(greenBar, greenRestColor);
        SetColor(yellowBar, yellowRestColor);
        SetColor(redBar, redRestColor);
    }

    private static void ApplyFeedbackColor(
        SpriteRenderer renderer,
        Color restingColor,
        float amount)
    {
        if (renderer == null)
            return;

        Color warningColor = new(1f, 0.08f, 0.05f, restingColor.a);
        renderer.color = Color.Lerp(restingColor, warningColor, amount * 0.82f);
    }

    private static Vector3 GetPosition(SpriteRenderer renderer)
    {
        return renderer != null ? renderer.transform.position : Vector3.zero;
    }

    private static void SetPosition(
        SpriteRenderer renderer,
        Vector3 position)
    {
        if (renderer != null)
            renderer.transform.position = position;
    }

    private static Color GetColor(SpriteRenderer renderer)
    {
        return renderer != null ? renderer.color : Color.white;
    }

    private static void SetColor(
        SpriteRenderer renderer,
        Color color)
    {
        if (renderer != null)
            renderer.color = color;
    }

    public void SetCoveredByTaskCard(
        bool covered,
        int taskCardSortingOrder)
    {
        CaptureSortingOrders();

        SpriteRenderer frameRenderer = GetComponent<SpriteRenderer>();
        if (covered)
        {
            int coveredOrder = taskCardSortingOrder - 1;
            SetSortingOrder(frameRenderer, coveredOrder);
            SetSortingOrder(greenBar, coveredOrder);
            SetSortingOrder(yellowBar, coveredOrder);
            SetSortingOrder(redBar, coveredOrder);
        }
        else
        {
            SetSortingOrder(frameRenderer, frameSortingOrder);
            SetSortingOrder(greenBar, greenSortingOrder);
            SetSortingOrder(yellowBar, yellowSortingOrder);
            SetSortingOrder(redBar, redSortingOrder);
        }

        Refresh();
    }

    private void CaptureSortingOrders()
    {
        if (sortingOrdersCaptured)
            return;

        SpriteRenderer frameRenderer = GetComponent<SpriteRenderer>();
        frameSortingOrder = GetSortingOrder(frameRenderer);
        greenSortingOrder = GetSortingOrder(greenBar);
        yellowSortingOrder = GetSortingOrder(yellowBar);
        redSortingOrder = GetSortingOrder(redBar);
        sortingOrdersCaptured = true;
    }

    private static int GetSortingOrder(SpriteRenderer renderer)
    {
        return renderer != null ? renderer.sortingOrder : 0;
    }

    private static void SetSortingOrder(
        SpriteRenderer renderer,
        int sortingOrder)
    {
        if (renderer != null)
            renderer.sortingOrder = sortingOrder;
    }

    private void EnsureFillMask()
    {
        if (fillMask != null)
            return;

        fillMaskTexture = new Texture2D(
            1,
            1,
            TextureFormat.RGBA32,
            false)
        {
            name = "Boss Anger Fill Mask Texture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        fillMaskTexture.SetPixel(0, 0, Color.white);
        fillMaskTexture.Apply(false, true);

        fillMaskSprite = Sprite.Create(
            fillMaskTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        fillMaskSprite.name = "Boss Anger Fill Mask Sprite";
        fillMaskSprite.hideFlags = HideFlags.HideAndDontSave;

        fillMaskObject = new GameObject("BossAngerFillMask")
        {
            hideFlags = HideFlags.DontSave
        };
        fillMaskObject.transform.SetParent(transform, false);
        fillMask = fillMaskObject.AddComponent<SpriteMask>();
        fillMask.sprite = fillMaskSprite;
        fillMask.alphaCutoff = 0f;
        fillMask.isCustomRangeActive = true;

        ConfigureRendererMask(greenBar);
        ConfigureRendererMask(yellowBar);
        ConfigureRendererMask(redBar);
    }

    private void UpdateFillMask(
        SpriteRenderer activeBar,
        float fillAmount,
        float contentLeft,
        float contentRight)
    {
        Transform maskTransform = fillMaskObject.transform;
        maskTransform.SetParent(activeBar.transform, false);

        Bounds spriteBounds = GetSpriteRectBounds(activeBar.sprite);
        float left = Mathf.Lerp(
            spriteBounds.min.x,
            spriteBounds.max.x,
            contentLeft);
        float fullWidth =
            spriteBounds.size.x * (contentRight - contentLeft);
        float visibleWidth = fullWidth * fillAmount;

        maskTransform.localPosition = new Vector3(
            left + (visibleWidth * 0.5f),
            spriteBounds.center.y,
            0f);
        maskTransform.localRotation = Quaternion.identity;
        maskTransform.localScale = new Vector3(
            visibleWidth,
            spriteBounds.size.y,
            1f);

        fillMask.frontSortingLayerID = activeBar.sortingLayerID;
        fillMask.backSortingLayerID = activeBar.sortingLayerID;
        fillMask.frontSortingOrder = activeBar.sortingOrder + 1;
        fillMask.backSortingOrder = activeBar.sortingOrder - 1;
        SetMaskVisible(visibleWidth > 0f);
    }

    private void AlignBarsToFrameTrack()
    {
        SpriteRenderer frameRenderer = GetComponent<SpriteRenderer>();
        if (frameRenderer == null || frameRenderer.sprite == null)
            return;

        AlignBar(frameRenderer, greenBar, GreenArtworkRect);
        AlignBar(frameRenderer, yellowBar, YellowArtworkRect);
        AlignBar(frameRenderer, redBar, RedArtworkRect);
    }

    private static void AlignBar(
        SpriteRenderer frameRenderer,
        SpriteRenderer barRenderer,
        Rect artworkRect)
    {
        if (barRenderer == null || barRenderer.sprite == null)
            return;

        Bounds frameBounds = GetSpriteRectBounds(frameRenderer.sprite);
        Bounds barBounds = GetSpriteRectBounds(barRenderer.sprite);
        Vector3 targetCenter = NormalizedPoint(
            frameBounds,
            FrameTrackRect.center);
        Vector3 artworkCenter = NormalizedPoint(
            barBounds,
            artworkRect.center);
        Vector2 targetSize = NormalizedSize(
            frameBounds,
            FrameTrackRect.size);
        Vector2 artworkSize = NormalizedSize(
            barBounds,
            artworkRect.size);

        if (artworkSize.x <= 0f || artworkSize.y <= 0f)
            return;

        Transform frameTransform = frameRenderer.transform;
        Transform barTransform = barRenderer.transform;
        Vector3 frameWorldScale = frameTransform.lossyScale;
        Vector3 desiredWorldScale = new(
            frameWorldScale.x * (targetSize.x / artworkSize.x),
            frameWorldScale.y * (targetSize.y / artworkSize.y),
            frameWorldScale.z);

        SetWorldScale(barTransform, desiredWorldScale);
        barTransform.rotation = frameTransform.rotation;

        Vector3 targetWorldPosition =
            frameTransform.TransformPoint(targetCenter);
        Vector3 artworkWorldOffset =
            barTransform.TransformVector(artworkCenter);
        barTransform.position =
            targetWorldPosition - artworkWorldOffset;
    }

    private static Vector3 NormalizedPoint(
        Bounds bounds,
        Vector2 normalizedPoint)
    {
        return new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, normalizedPoint.x),
            Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedPoint.y),
            bounds.center.z);
    }

    private static Bounds GetSpriteRectBounds(Sprite sprite)
    {
        float pixelsPerUnit = Mathf.Max(1f, sprite.pixelsPerUnit);
        Vector2 pivot = sprite.pivot;
        Rect rect = sprite.rect;
        Bounds bounds = new();
        bounds.SetMinMax(
            new Vector3(
                -pivot.x / pixelsPerUnit,
                -pivot.y / pixelsPerUnit,
                0f),
            new Vector3(
                (rect.width - pivot.x) / pixelsPerUnit,
                (rect.height - pivot.y) / pixelsPerUnit,
                0f));
        return bounds;
    }

    private static Vector2 NormalizedSize(
        Bounds bounds,
        Vector2 normalizedSize)
    {
        return new Vector2(
            bounds.size.x * normalizedSize.x,
            bounds.size.y * normalizedSize.y);
    }

    private static void SetWorldScale(
        Transform target,
        Vector3 desiredWorldScale)
    {
        Vector3 parentScale = target.parent != null
            ? target.parent.lossyScale
            : Vector3.one;
        target.localScale = new Vector3(
            SafeDivide(desiredWorldScale.x, parentScale.x),
            SafeDivide(desiredWorldScale.y, parentScale.y),
            SafeDivide(desiredWorldScale.z, parentScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Approximately(divisor, 0f)
            ? value
            : value / divisor;
    }

    private void ConfigureRendererMask(SpriteRenderer renderer)
    {
        if (renderer != null)
        {
            renderer.maskInteraction =
                SpriteMaskInteraction.VisibleInsideMask;
        }
    }

    private void SetMaskVisible(bool visible)
    {
        if (fillMaskObject != null)
            fillMaskObject.SetActive(visible);
    }

    private static void SetVisible(
        SpriteRenderer renderer,
        bool visible)
    {
        if (renderer != null)
            renderer.enabled = visible;
    }

#if UNITY_EDITOR
    public void ConfigureEditor(
        SpriteRenderer configuredGreenBar,
        SpriteRenderer configuredYellowBar,
        SpriteRenderer configuredRedBar)
    {
        greenBar = configuredGreenBar;
        yellowBar = configuredYellowBar;
        redBar = configuredRedBar;
    }
#endif
}
