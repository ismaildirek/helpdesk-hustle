using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WifiSignalSceneBuilder
{
    private const string SceneName = "wifi_sinyal";
    private const string ScenePath = "Assets/Scenes/wifi_sinyal.unity";
    private const string RootName = "WifiSignalMiniGameSystem";
    private const string BackIconGuid = "b49b25308b432cc4b8ad026fd058d890";
    private const string BackgroundGuid = "90c5b5920f45cde47b47019052260ce5";
    private const string DeviceGuid = "2cf6cf4b64671d0409e20fe43f14335d";
    private const string DeviceAuraGuid = "9f33a9866b27bfa49981e2b50a4a91fd";
    private const string NoSignalGuid = "5ce92419801212a44ae74e62a814644b";
    private const string ConnectedSignalGuid = "55ec162e5e088d34da2ebcdabd2aad85";

    private static readonly Vector2[] DeskPositions =
    {
        new(0.205f, 0.865f),
        new(0.795f, 0.785f),
        new(0.145f, 0.615f),
        new(0.805f, 0.305f)
    };

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TryBuild;
    }

    [MenuItem("Tools/Mini Games/Build Wi-Fi Signal Scene")]
    public static void BuildFromMenu()
    {
        Build(true);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += TryBuild;
        }
    }

    private static void TryBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene originalActiveScene = SceneManager.GetActiveScene();
        Scene wifiScene = SceneManager.GetSceneByPath(ScenePath);
        bool wasLoaded = wifiScene.IsValid() && wifiScene.isLoaded;

        if (!wasLoaded)
        {
            wifiScene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
        }

        SceneManager.SetActiveScene(wifiScene);

        GameObject existingRoot = FindRoot(wifiScene, RootName);
        if (existingRoot != null &&
            existingRoot.GetComponent<WifiSignalMiniGame>() != null)
        {
            EnsureBackIcon(wifiScene, Camera.main);
            AddSceneToBuildSettings();
            EditorSceneManager.MarkSceneDirty(wifiScene);
            EditorSceneManager.SaveScene(wifiScene);
        }
        else
        {
            Build(false);
        }

        if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(originalActiveScene);
        }

        if (!wasLoaded)
        {
            EditorSceneManager.CloseScene(wifiScene, true);
        }
    }

    private static void Build(bool showDialog)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SceneName)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Wi-Fi Signal",
                    "Open the wifi_sinyal scene first.",
                    "OK");
            }

            return;
        }

        Camera gameCamera = Camera.main;
        if (gameCamera == null)
        {
            Debug.LogError("wifi_sinyal scene needs a Main Camera.");
            return;
        }

        Sprite backgroundSprite = LoadLargestSprite(BackgroundGuid);
        Sprite deviceSprite = LoadLargestSprite(DeviceGuid);
        Sprite auraSprite = LoadLargestSprite(DeviceAuraGuid);
        Sprite noSignalSprite = LoadLargestSprite(NoSignalGuid);
        Sprite connectedSprite = LoadLargestSprite(ConnectedSignalGuid);

        if (backgroundSprite == null || deviceSprite == null ||
            noSignalSprite == null || connectedSprite == null)
        {
            Debug.LogError(
                "One or more Wi-Fi mini game sprites could not be loaded from wifi_sinyal_as.");
            return;
        }

        GameObject existingRoot = FindRoot(scene, RootName);
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
        }

        GameObject root = new(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Wi-Fi Signal Mini Game");

        SpriteRenderer background = CreateRenderer(
            "WifiSignalBackground",
            root.transform,
            backgroundSprite,
            -100);
        background.transform.position = Vector3.zero;
        background.transform.localScale = Vector3.one;

        ConfigureCamera(gameCamera, background);

        List<WifiSignalMiniGame.DeskTarget> targets = new();
        Bounds backgroundBounds = background.bounds;

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
                connectedSprite,
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

        Vector3 deviceStart = NormalizedToWorld(
            backgroundBounds,
            new Vector2(0.5f, 0.475f),
            -1f);

        GameObject deviceRoot = new("wifi_esle");
        deviceRoot.transform.SetParent(root.transform, false);
        deviceRoot.transform.position = deviceStart;

        if (auraSprite != null)
        {
            SpriteRenderer aura = CreateRenderer(
                "wifi_esle_sinyal_halkasi",
                deviceRoot.transform,
                auraSprite,
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

        EnsureBackIcon(scene, gameCamera);
        AddSceneToBuildSettings();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        Debug.Log(
            "Wi-Fi Signal mini game ready: four desk targets, device reset after every drop, direct return to YeniOfis on completion.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Wi-Fi Signal",
                "Wi-Fi Signal mini game is ready.",
                "OK");
        }
    }

    private static void ConfigureCamera(Camera gameCamera, SpriteRenderer background)
    {
        gameCamera.orthographic = true;
        gameCamera.transform.position = new Vector3(0f, 0f, -10f);
        gameCamera.orthographicSize = background.sprite.bounds.extents.y;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color32(22, 31, 49, 255);
        EditorUtility.SetDirty(gameCamera);
    }

    private static void EnsureBackIcon(Scene scene, Camera gameCamera)
    {
        if (gameCamera == null)
        {
            return;
        }

        Sprite backSprite = LoadLargestSprite(BackIconGuid);
        if (backSprite == null)
        {
            Debug.LogError("geri_ikon sprite is missing.");
            return;
        }

        SpriteRenderer backIcon = scene
            .GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
            .FirstOrDefault(renderer =>
                renderer.name.Equals("Geri_ikon", StringComparison.OrdinalIgnoreCase) ||
                renderer.name.Equals("geri_ikon", StringComparison.OrdinalIgnoreCase) ||
                renderer.sprite == backSprite);

        if (backIcon == null)
        {
            GameObject backObject = new("Geri_ikon");
            backIcon = backObject.AddComponent<SpriteRenderer>();
            backIcon.sprite = backSprite;
            SetRenderedHeight(backIcon, 3.3f);

            float halfHeight = gameCamera.orthographicSize;
            float halfWidth = halfHeight * gameCamera.aspect;
            backObject.transform.position = new Vector3(
                gameCamera.transform.position.x - halfWidth + 2.1f,
                gameCamera.transform.position.y + halfHeight - 2.1f,
                -2f);

            Undo.RegisterCreatedObjectUndo(backObject, "Create Wi-Fi Back Icon");
        }

        backIcon.sortingOrder = 100;
        backIcon.gameObject.SetActive(true);

        SceneIconButton button = backIcon.GetComponent<SceneIconButton>();
        if (button == null)
        {
            button = Undo.AddComponent<SceneIconButton>(backIcon.gameObject);
        }

        button.Configure("katlar");
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(backIcon);
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

    private static Sprite LoadLargestSprite(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        return AssetDatabase
            .LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderByDescending(sprite => sprite.bounds.size.x * sprite.bounds.size.y)
            .FirstOrDefault();
    }

    private static GameObject FindRoot(Scene scene, string rootName)
    {
        return scene
            .GetRootGameObjects()
            .FirstOrDefault(root => root.name == rootName);
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        int existingIndex = Array.FindIndex(
            scenes,
            item => item.path.Equals(ScenePath, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            if (!scenes[existingIndex].enabled)
            {
                scenes[existingIndex] = new EditorBuildSettingsScene(ScenePath, true);
                EditorBuildSettings.scenes = scenes;
            }

            return;
        }

        EditorBuildSettings.scenes = scenes
            .Append(new EditorBuildSettingsScene(ScenePath, true))
            .ToArray();
    }
}
