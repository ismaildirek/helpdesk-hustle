using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WifiSignalRuntimeBootstrap : MonoBehaviour
{
    private const string RootName = "WifiSignalMiniGameSystem";

    private static readonly Vector2[] DeskPositions =
    {
        new(0.205f, 0.865f),
        new(0.795f, 0.785f),
        new(0.145f, 0.615f),
        new(0.805f, 0.305f)
    };

    [SerializeField] private Sprite backgroundSprite = null;
    [SerializeField] private Sprite deviceSprite = null;
    [SerializeField] private Sprite deviceAuraSprite = null;
    [SerializeField] private Sprite noSignalSprite = null;
    [SerializeField] private Sprite connectedSignalSprite = null;
    [SerializeField] private Sprite backIconSprite = null;

    private void Awake()
    {
        if (FindFirstObjectByType<WifiSignalMiniGame>() != null)
        {
            return;
        }

        if (backgroundSprite == null || deviceSprite == null ||
            noSignalSprite == null || connectedSignalSprite == null)
        {
            Debug.LogError(
                "Wi-Fi mini game scene assets are missing from its runtime bootstrap.",
                this);
            return;
        }

        Camera gameCamera = GetComponent<Camera>();
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }

        if (gameCamera == null)
        {
            Debug.LogError("Wi-Fi mini game needs a Main Camera.", this);
            return;
        }

        BuildGame(gameCamera);
    }

    private void BuildGame(Camera gameCamera)
    {
        GameObject root = new(RootName);

        SpriteRenderer background = CreateRenderer(
            "WifiSignalBackground",
            root.transform,
            backgroundSprite,
            -100);

        gameCamera.orthographic = true;
        gameCamera.transform.position = new Vector3(0f, 0f, -10f);
        gameCamera.orthographicSize = backgroundSprite.bounds.extents.y;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color32(22, 31, 49, 255);

        Bounds backgroundBounds = background.bounds;
        List<WifiSignalMiniGame.DeskTarget> targets = new();

        for (int index = 0; index < DeskPositions.Length; index++)
        {
            GameObject targetRoot = new($"Desk_{index + 1}_SignalTarget");
            targetRoot.transform.SetParent(root.transform, false);
            targetRoot.transform.position = NormalizedToWorld(
                backgroundBounds,
                DeskPositions[index],
                -0.5f);

            SpriteRenderer noSignal = CreateRenderer(
                "sinyal_yok",
                targetRoot.transform,
                noSignalSprite,
                20);
            SetRenderedHeight(noSignal, 5.8f);

            SpriteRenderer connectedSignal = CreateRenderer(
                "wifi_baglanti_tamam",
                targetRoot.transform,
                connectedSignalSprite,
                21);
            SetRenderedHeight(connectedSignal, 5.8f);
            connectedSignal.enabled = false;

            WifiSignalMiniGame.DeskTarget target = new();
            target.Configure(
                targetRoot.transform,
                noSignal,
                connectedSignal,
                2.25f);
            targets.Add(target);
        }

        GameObject deviceRoot = new("wifi_esle");
        deviceRoot.transform.SetParent(root.transform, false);
        deviceRoot.transform.position = NormalizedToWorld(
            backgroundBounds,
            new Vector2(0.5f, 0.475f),
            -1f);

        if (deviceAuraSprite != null)
        {
            SpriteRenderer aura = CreateRenderer(
                "wifi_esle_sinyal_halkasi",
                deviceRoot.transform,
                deviceAuraSprite,
                29);
            SetRenderedHeight(aura, 7.2f);
            aura.color = new Color(1f, 1f, 1f, 0.72f);
        }

        SpriteRenderer device = CreateRenderer(
            "wifi_esle_gorsel",
            deviceRoot.transform,
            deviceSprite,
            30);
        SetRenderedHeight(device, 5.5f);

        WifiSignalMiniGame controller = root.AddComponent<WifiSignalMiniGame>();
        controller.Configure(
            gameCamera,
            deviceRoot.transform,
            device,
            targets.ToArray(),
            2.1f,
            0.4f,
            "YeniOfis",
            0.45f);

        EnsureBackIcon(gameCamera);
    }

    private void EnsureBackIcon(Camera gameCamera)
    {
        SpriteRenderer backIcon = FindObjectsByType<SpriteRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(renderer =>
                renderer.name.Equals("Geri_ikon", StringComparison.OrdinalIgnoreCase) ||
                renderer.name.Equals("geri_ikon", StringComparison.OrdinalIgnoreCase));

        if (backIcon == null && backIconSprite != null)
        {
            GameObject backObject = new("Geri_ikon");
            backIcon = backObject.AddComponent<SpriteRenderer>();
            backIcon.sprite = backIconSprite;
            SetRenderedHeight(backIcon, 3.3f);

            float halfHeight = gameCamera.orthographicSize;
            float halfWidth = halfHeight * gameCamera.aspect;
            backObject.transform.position = new Vector3(
                gameCamera.transform.position.x - halfWidth + 2.1f,
                gameCamera.transform.position.y + halfHeight - 2.1f,
                -2f);
        }

        if (backIcon == null)
        {
            Debug.LogError("Wi-Fi mini game geri_ikon could not be found.", this);
            return;
        }

        backIcon.sortingOrder = 100;
        backIcon.gameObject.SetActive(true);

        SceneIconButton button = backIcon.GetComponent<SceneIconButton>();
        if (button == null)
        {
            button = backIcon.gameObject.AddComponent<SceneIconButton>();
        }

        button.Configure("katlar");
    }

    private static SpriteRenderer CreateRenderer(
        string objectName,
        Transform parent,
        Sprite sprite,
        int sortingOrder)
    {
        GameObject gameObject = new(objectName);
        gameObject.transform.SetParent(parent, false);
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void SetRenderedHeight(SpriteRenderer renderer, float height)
    {
        float spriteHeight = Mathf.Max(0.001f, renderer.sprite.bounds.size.y);
        float scale = height / spriteHeight;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static Vector3 NormalizedToWorld(
        Bounds bounds,
        Vector2 normalizedPosition,
        float z)
    {
        return new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, normalizedPosition.x),
            Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedPosition.y),
            z);
    }
}
