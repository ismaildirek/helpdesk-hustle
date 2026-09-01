using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class OfficeAmbientJuice : MonoBehaviour
{
    private readonly struct GlowLayout
    {
        public GlowLayout(
            float x,
            float y,
            float width,
            float height,
            float strength,
            float phase)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Strength = strength;
            Phase = phase;
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public float Strength { get; }
        public float Phase { get; }
    }

    private sealed class MonitorGlow
    {
        public SpriteRenderer Renderer;
        public Vector3 RestScale;
        public float Strength;
        public float Phase;
    }

    private sealed class SteamPuff
    {
        public SpriteRenderer Renderer;
        public Vector3 RestPosition;
        public float Phase;
    }

    private static readonly GlowLayout[] GlowLayouts =
    {
        new(0.29f, 0.931f, 0.19f, 0.066f, 0.55f, 0.1f),
        new(0.505f, 0.931f, 0.18f, 0.066f, 0.48f, 1.4f),
        new(0.72f, 0.931f, 0.19f, 0.066f, 0.54f, 2.7f),
        new(0.29f, 0.845f, 0.19f, 0.065f, 0.45f, 3.8f),
        new(0.505f, 0.845f, 0.18f, 0.065f, 0.5f, 5.1f),
        new(0.72f, 0.845f, 0.19f, 0.065f, 0.46f, 6.2f),
        new(0.31f, 0.768f, 0.074f, 0.022f, 0.75f, 0.7f),
        new(0.425f, 0.768f, 0.074f, 0.022f, 0.72f, 2.2f),
        new(0.61f, 0.768f, 0.074f, 0.022f, 0.76f, 3.6f),
        new(0.72f, 0.768f, 0.074f, 0.022f, 0.7f, 4.9f),
        new(0.242f, 0.64f, 0.084f, 0.026f, 0.82f, 1.1f),
        new(0.75f, 0.64f, 0.084f, 0.026f, 0.8f, 2.9f),
        new(0.242f, 0.515f, 0.084f, 0.026f, 0.78f, 4.4f),
        new(0.75f, 0.515f, 0.084f, 0.026f, 0.82f, 5.8f),
        new(0.242f, 0.395f, 0.084f, 0.026f, 0.76f, 0.3f),
        new(0.75f, 0.395f, 0.084f, 0.026f, 0.8f, 3.3f)
    };

    [Header("Monitor Lighting")]
    [SerializeField] private Color monitorGlowColor =
        new(0.08f, 0.72f, 1f, 1f);
    [SerializeField, Range(0f, 0.3f)] private float minimumAlpha = 0.015f;
    [SerializeField, Range(0f, 0.4f)] private float maximumAlpha = 0.12f;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 1.45f;

    [Header("Office Props")]
    [SerializeField] private Sprite printerSprite = null;
    [SerializeField] private Sprite coffeeMachineSprite = null;
    [SerializeField] private Sprite phoneSprite = null;

    private static Sprite glowSprite;
    private readonly List<MonitorGlow> monitorGlows = new();
    private readonly List<SteamPuff> steamPuffs = new();
    private SpriteRenderer backgroundRenderer;
    private SpriteRenderer printerRenderer;
    private SpriteRenderer phoneNotification;
    private SpriteRenderer scanLineRenderer;
    private Vector3 printerRestPosition;
    private Vector3 phoneNotificationRestScale;
    private Vector3 scanLineRestScale;
    private Bounds backgroundSpriteBounds;

    private void Awake()
    {
        backgroundRenderer = GetComponent<SpriteRenderer>();
        BuildMonitorGlows();
        BuildOfficeProps();
        BuildUpperScreenActivity();
    }

    private void Update()
    {
        float time = Time.unscaledTime * pulseSpeed;

        foreach (MonitorGlow glow in monitorGlows)
        {
            if (glow.Renderer == null)
            {
                continue;
            }

            float wave = 0.5f +
                0.5f * Mathf.Sin(time + glow.Phase);
            float flicker = 0.88f +
                0.12f * Mathf.PerlinNoise(
                    glow.Phase,
                    Time.unscaledTime * 2.2f);
            float alpha = Mathf.Lerp(
                minimumAlpha,
                maximumAlpha,
                wave) * glow.Strength * flicker;

            glow.Renderer.color = new Color(
                monitorGlowColor.r,
                monitorGlowColor.g,
                monitorGlowColor.b,
                alpha);
            glow.Renderer.transform.localScale = Vector3.Scale(
                glow.RestScale,
                new Vector3(
                    1f + wave * 0.018f,
                    1f + wave * 0.012f,
                    1f));
        }

        AnimatePrinter();
        AnimateSteam();
        AnimatePhoneNotification();
        AnimateUpperScreenActivity();
    }

    private void BuildMonitorGlows()
    {
        if (backgroundRenderer == null ||
            backgroundRenderer.sprite == null)
        {
            enabled = false;
            return;
        }

        EnsureGlowSprite();
        backgroundSpriteBounds = backgroundRenderer.sprite.bounds;

        for (int index = 0; index < GlowLayouts.Length; index++)
        {
            GlowLayout layout = GlowLayouts[index];
            GameObject glowObject = new($"MonitorGlow_{index + 1:00}");
            glowObject.transform.SetParent(transform, false);
            glowObject.transform.localPosition = new Vector3(
                Mathf.Lerp(
                    backgroundSpriteBounds.min.x,
                    backgroundSpriteBounds.max.x,
                    layout.X),
                Mathf.Lerp(
                    backgroundSpriteBounds.min.y,
                    backgroundSpriteBounds.max.y,
                    layout.Y),
                -0.01f);
            glowObject.transform.localScale = new Vector3(
                backgroundSpriteBounds.size.x * layout.Width,
                backgroundSpriteBounds.size.y * layout.Height,
                1f);

            SpriteRenderer glowRenderer =
                glowObject.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = glowSprite;
            glowRenderer.sortingLayerID = backgroundRenderer.sortingLayerID;
            glowRenderer.sortingOrder = backgroundRenderer.sortingOrder + 1;
            glowRenderer.color = new Color(
                monitorGlowColor.r,
                monitorGlowColor.g,
                monitorGlowColor.b,
                0f);

            monitorGlows.Add(new MonitorGlow
            {
                Renderer = glowRenderer,
                RestScale = glowObject.transform.localScale,
                Strength = layout.Strength,
                Phase = layout.Phase
            });
        }
    }

    private void BuildOfficeProps()
    {
        if (backgroundRenderer == null ||
            backgroundRenderer.sprite == null)
        {
            return;
        }

        if (backgroundSpriteBounds.size.sqrMagnitude <= 0f)
            backgroundSpriteBounds = backgroundRenderer.sprite.bounds;

        printerRenderer = CreateProp(
            "AmbientPrinter",
            printerSprite,
            new Vector2(0.105f, 0.61f),
            0.044f);
        if (printerRenderer != null)
            printerRestPosition = printerRenderer.transform.localPosition;

        SpriteRenderer coffeeRenderer = CreateProp(
            "AmbientCoffeeMachine",
            coffeeMachineSprite,
            new Vector2(0.875f, 0.205f),
            0.062f);
        SpriteRenderer phoneRenderer = CreateProp(
            "AmbientPhone",
            phoneSprite,
            new Vector2(0.82f, 0.762f),
            0.027f);

        if (coffeeRenderer != null)
            BuildSteam(coffeeRenderer.transform.localPosition);

        if (phoneRenderer != null)
            BuildPhoneNotification(phoneRenderer.transform.localPosition);
    }

    private SpriteRenderer CreateProp(
        string objectName,
        Sprite sprite,
        Vector2 normalizedPosition,
        float normalizedHeight)
    {
        if (sprite == null)
            return null;

        GameObject propObject = new(objectName);
        propObject.transform.SetParent(transform, false);
        Vector3 targetCenter = LocalPoint(normalizedPosition, -0.02f);

        float height = backgroundSpriteBounds.size.y * normalizedHeight;
        float scale = height / Mathf.Max(0.001f, sprite.bounds.size.y);
        propObject.transform.localScale = new Vector3(scale, scale, 1f);
        propObject.transform.localPosition = targetCenter -
            Vector3.Scale(
                sprite.bounds.center,
                propObject.transform.localScale);

        SpriteRenderer renderer = propObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = backgroundRenderer.sortingLayerID;
        renderer.sortingOrder = backgroundRenderer.sortingOrder + 3;
        return renderer;
    }

    private void BuildSteam(Vector3 coffeePosition)
    {
        EnsureGlowSprite();
        for (int index = 0; index < 3; index++)
        {
            GameObject puffObject = new($"CoffeeSteam_{index + 1}");
            puffObject.transform.SetParent(transform, false);
            Vector3 restPosition = coffeePosition +
                new Vector3(0f, 1.75f, -0.03f);
            puffObject.transform.localPosition = restPosition;
            puffObject.transform.localScale = Vector3.one * 0.2f;

            SpriteRenderer renderer = puffObject.AddComponent<SpriteRenderer>();
            renderer.sprite = glowSprite;
            renderer.sortingLayerID = backgroundRenderer.sortingLayerID;
            renderer.sortingOrder = backgroundRenderer.sortingOrder + 4;
            renderer.color = new Color(0.82f, 0.96f, 1f, 0f);

            steamPuffs.Add(new SteamPuff
            {
                Renderer = renderer,
                RestPosition = restPosition,
                Phase = index / 3f
            });
        }
    }

    private void BuildPhoneNotification(Vector3 phonePosition)
    {
        EnsureGlowSprite();
        GameObject notificationObject = new("PhoneNotification");
        notificationObject.transform.SetParent(transform, false);
        notificationObject.transform.localPosition = phonePosition +
            new Vector3(0.72f, 1.05f, -0.04f);
        notificationObject.transform.localScale = Vector3.one * 0.28f;

        phoneNotification =
            notificationObject.AddComponent<SpriteRenderer>();
        phoneNotification.sprite = glowSprite;
        phoneNotification.sortingLayerID = backgroundRenderer.sortingLayerID;
        phoneNotification.sortingOrder = backgroundRenderer.sortingOrder + 5;
        phoneNotification.color = new Color(1f, 0.28f, 0.18f, 0f);
        phoneNotificationRestScale = notificationObject.transform.localScale;
    }

    private void BuildUpperScreenActivity()
    {
        if (backgroundRenderer == null ||
            backgroundRenderer.sprite == null)
        {
            return;
        }

        EnsureGlowSprite();
        GameObject scanObject = new("UpperDisplayScanLine");
        scanObject.transform.SetParent(transform, false);
        scanObject.transform.localPosition = LocalPoint(
            new Vector2(0.19f, 0.89f),
            -0.035f);
        scanObject.transform.localScale = new Vector3(
            backgroundSpriteBounds.size.x * 0.006f,
            backgroundSpriteBounds.size.y * 0.135f,
            1f);
        scanLineRenderer = scanObject.AddComponent<SpriteRenderer>();
        scanLineRenderer.sprite = glowSprite;
        scanLineRenderer.sortingLayerID = backgroundRenderer.sortingLayerID;
        scanLineRenderer.sortingOrder = backgroundRenderer.sortingOrder + 4;
        scanLineRenderer.color = new Color(0.35f, 0.96f, 1f, 0.08f);
        scanLineRestScale = scanObject.transform.localScale;

    }

    private void AnimatePrinter()
    {
        if (printerRenderer == null)
            return;

        float cycle = Mathf.Repeat(Time.unscaledTime, 5.2f);
        float activity = cycle < 0.65f
            ? Mathf.Sin(cycle / 0.65f * Mathf.PI)
            : 0f;
        printerRenderer.transform.localPosition = printerRestPosition +
            Vector3.right *
            Mathf.Sin(cycle * 58f) * activity * 0.13f;
        printerRenderer.transform.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Sin(cycle * 42f) * activity * 1.2f);
    }

    private void AnimateSteam()
    {
        foreach (SteamPuff puff in steamPuffs)
        {
            if (puff.Renderer == null)
                continue;

            float progress = Mathf.Repeat(
                Time.unscaledTime * 0.28f + puff.Phase,
                1f);
            float alpha = Mathf.Sin(progress * Mathf.PI) * 0.2f;
            puff.Renderer.transform.localPosition = puff.RestPosition +
                new Vector3(
                    Mathf.Sin(progress * Mathf.PI * 2f + puff.Phase * 5f) * 0.18f,
                    progress * 1.7f,
                    0f);
            puff.Renderer.transform.localScale = Vector3.one *
                Mathf.Lerp(0.14f, 0.34f, progress);
            puff.Renderer.color = new Color(0.82f, 0.96f, 1f, alpha);
        }
    }

    private void AnimatePhoneNotification()
    {
        if (phoneNotification == null)
            return;

        float cycle = Mathf.Repeat(Time.unscaledTime, 4.1f);
        float pulse = cycle < 0.85f
            ? Mathf.Abs(Mathf.Sin(cycle / 0.85f * Mathf.PI * 3f))
            : 0f;
        phoneNotification.color = new Color(
            1f,
            0.24f,
            0.12f,
            pulse * 0.82f);
        phoneNotification.transform.localScale =
            phoneNotificationRestScale * (1f + pulse * 0.45f);
    }

    private void AnimateUpperScreenActivity()
    {
        float scroll = Mathf.Repeat(Time.unscaledTime / 7.5f, 1f);
        if (scanLineRenderer != null)
        {
            Vector3 position = LocalPoint(
                new Vector2(Mathf.Lerp(0.19f, 0.81f, scroll), 0.89f),
                -0.035f);
            scanLineRenderer.transform.localPosition = position;
            float pulse = 0.72f +
                Mathf.Sin(Time.unscaledTime * 3.4f) * 0.18f;
            scanLineRenderer.transform.localScale = Vector3.Scale(
                scanLineRestScale,
                new Vector3(pulse, 1f, 1f));
        }

    }

    private Vector3 LocalPoint(Vector2 normalized, float z)
    {
        return new Vector3(
            Mathf.Lerp(
                backgroundSpriteBounds.min.x,
                backgroundSpriteBounds.max.x,
                normalized.x),
            Mathf.Lerp(
                backgroundSpriteBounds.min.y,
                backgroundSpriteBounds.max.y,
                normalized.y),
            z);
    }

    private static void EnsureGlowSprite()
    {
        if (glowSprite != null)
        {
            return;
        }

        glowSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        glowSprite.name = "RuntimeMonitorGlow";
        glowSprite.hideFlags = HideFlags.HideAndDontSave;
    }
}
