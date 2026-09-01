using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BrokenMonitorRepairSceneBuilder
{
    private const string SceneName = "bozukmonit\u00F6r";
    private const string ScenePath =
        "Assets/Scenes/bozukmonit\u00F6r.unity";
    private const string RootName =
        "BrokenMonitorRepairSystem";
    private const string MarkerName =
        "BrokenMonitorRepair_v1";
    private const string ArtFolder =
        "Assets/Art/UI/kasa";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TryBuild;
    }

    [MenuItem(
        "Tools/Mini Games/Build Broken Monitor Repair Scene")]
    public static void BuildFromMenu()
    {
        Build(true);
    }

    private static void TryBuild()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SceneName ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        GameObject root = GameObject.Find(RootName);
        if (root != null &&
            root.transform.Find(MarkerName) != null)
        {
            return;
        }

        Build(false);
    }

    private static void OnPlayModeStateChanged(
        PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += TryBuild;
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
                    "Broken Monitor Repair",
                    "Open the bozukmonit\u00F6r scene first.",
                    "OK");
            }

            return;
        }

        Camera gameCamera = Camera.main;
        if (gameCamera == null)
        {
            Debug.LogError(
                "Broken Monitor Repair scene needs a Main Camera.");
            return;
        }

        Sprite brokenSprite = LoadSprite("monitor_bozuk");
        Sprite repairedSprite = LoadSprite("monitor_saglam");
        Sprite handSprite = LoadSprite("tokat_eli");
        Sprite backgroundSprite =
            LoadSprite("minioyun_arkaplan_sade");

        Sprite[] effectSprites =
        {
            LoadSprite("vurus_efektleri"),
            LoadSprite("az_patlama"),
            LoadSprite("patlama"),
            LoadSprite("yo\u011Fun_patlama")
        };

        if (brokenSprite == null ||
            repairedSprite == null ||
            handSprite == null ||
            effectSprites.Any(sprite => sprite == null))
        {
            Debug.LogError(
                "Broken Monitor Repair assets are missing from Assets/Art/UI/kasa.");
            return;
        }

        SpriteRenderer brokenMonitor =
            EnsureRenderer(
                scene,
                "monitor_bozuk",
                brokenSprite);
        SpriteRenderer repairedMonitor =
            EnsureRenderer(
                scene,
                "monitor_saglam",
                repairedSprite);
        SpriteRenderer hand =
            EnsureRenderer(
                scene,
                "tokat_eli",
                handSprite);
        SpriteRenderer hitEffect =
            EnsureRenderer(
                scene,
                "monitor_hit_effect",
                effectSprites[0]);

        bool brokenWasPlaced =
            brokenMonitor.transform.localScale != Vector3.one ||
            brokenMonitor.transform.position != Vector3.zero;

        if (!brokenWasPlaced)
        {
            FitSpriteHeight(
                brokenMonitor,
                gameCamera.orthographicSize * 0.95f);
            brokenMonitor.transform.position =
                new Vector3(
                    gameCamera.transform.position.x,
                    gameCamera.transform.position.y - 0.25f,
                    0f);
        }

        repairedMonitor.transform.SetPositionAndRotation(
            brokenMonitor.transform.position,
            brokenMonitor.transform.rotation *
            Quaternion.Euler(0f, 180f, 0f));

        float repairedScale =
            brokenMonitor.bounds.size.y /
            repairedSprite.bounds.size.y;
        repairedMonitor.transform.localScale =
            Vector3.one * repairedScale;

        bool handWasPlaced =
            hand.transform.localScale != Vector3.one ||
            hand.transform.position != Vector3.zero;

        if (!handWasPlaced)
        {
            FitSpriteWidth(hand, 3f);
            hand.transform.position =
                new Vector3(
                    brokenMonitor.bounds.max.x + 1.1f,
                    brokenMonitor.bounds.min.y + 0.7f,
                    -0.5f);
        }

        FitSpriteWidth(hitEffect, 2.2f);
        hitEffect.transform.position =
            new Vector3(
                brokenMonitor.bounds.center.x + 0.35f,
                brokenMonitor.bounds.center.y + 0.35f,
                -1f);

        brokenMonitor.sortingOrder = 10;
        repairedMonitor.sortingOrder = 10;
        hand.sortingOrder = 20;
        hitEffect.sortingOrder = 30;

        brokenMonitor.gameObject.SetActive(true);
        repairedMonitor.gameObject.SetActive(true);
        hand.gameObject.SetActive(true);
        hitEffect.gameObject.SetActive(true);

        repairedMonitor.enabled = false;
        hitEffect.enabled = false;

        DisableRendererByName(scene, "kasa_tamir");
        DisableRendererByName(scene, "pc_kasa_saglam");

        EnsureBackground(
            scene,
            gameCamera,
            backgroundSprite);

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
        }

        GameObject root = new(RootName);
        Undo.RegisterCreatedObjectUndo(
            root,
            "Build Broken Monitor Repair");

        GameObject marker = new(MarkerName);
        marker.transform.SetParent(root.transform, false);

        BrokenPcRepairController controller =
            root.AddComponent<BrokenPcRepairController>();

        controller.Configure(
            brokenMonitor,
            repairedMonitor,
            hand,
            hitEffect,
            effectSprites,
            "YeniOfis");

        AddSceneToBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        Debug.Log(
            "Broken Monitor Repair ready: five hits, effects and YeniOfis completion.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Broken Monitor Repair",
                "Broken Monitor Repair mini game is ready.",
                "OK");
        }
    }

    private static SpriteRenderer EnsureRenderer(
        Scene scene,
        string objectName,
        Sprite sprite)
    {
        SpriteRenderer renderer = scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<SpriteRenderer>(true))
            .FirstOrDefault(item =>
                item.sprite == sprite ||
                item.gameObject.name.Equals(
                    objectName,
                    StringComparison.OrdinalIgnoreCase));

        if (renderer != null)
        {
            renderer.sprite = sprite;
            return renderer;
        }

        GameObject newObject = new(objectName);
        Undo.RegisterCreatedObjectUndo(
            newObject,
            $"Create {objectName}");

        renderer = newObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        return renderer;
    }

    private static void DisableRendererByName(
        Scene scene,
        string objectName)
    {
        SpriteRenderer renderer = scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<SpriteRenderer>(true))
            .FirstOrDefault(item =>
                item.name.Equals(
                    objectName,
                    StringComparison.OrdinalIgnoreCase));

        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }

    private static void EnsureBackground(
        Scene scene,
        Camera gameCamera,
        Sprite backgroundSprite)
    {
        if (backgroundSprite == null)
        {
            return;
        }

        SpriteRenderer background =
            EnsureRenderer(
                scene,
                "minioyun_arkaplan_sade",
                backgroundSprite);

        float worldHeight = gameCamera.orthographicSize * 2f;
        float worldWidth = worldHeight * gameCamera.aspect;
        float scale = Mathf.Max(
            worldWidth / backgroundSprite.bounds.size.x,
            worldHeight / backgroundSprite.bounds.size.y);

        background.transform.position =
            new Vector3(
                gameCamera.transform.position.x,
                gameCamera.transform.position.y,
                2f);
        background.transform.localScale =
            Vector3.one * scale;
        background.sortingOrder = -100;
        background.gameObject.SetActive(true);
    }

    private static void FitSpriteHeight(
        SpriteRenderer renderer,
        float desiredHeight)
    {
        float scale =
            desiredHeight / renderer.sprite.bounds.size.y;
        renderer.transform.localScale =
            Vector3.one * scale;
    }

    private static void FitSpriteWidth(
        SpriteRenderer renderer,
        float desiredWidth)
    {
        float scale =
            desiredWidth / renderer.sprite.bounds.size.x;
        renderer.transform.localScale =
            Vector3.one * scale;
    }

    private static Sprite LoadSprite(string assetName)
    {
        string guid = AssetDatabase
            .FindAssets(
                $"{assetName} t:Sprite",
                new[] { ArtFolder })
            .FirstOrDefault();

        if (string.IsNullOrEmpty(guid))
        {
            return null;
        }

        string path =
            AssetDatabase.GUIDToAssetPath(guid);

        return AssetDatabase
            .LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault();
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] scenes =
            EditorBuildSettings.scenes;

        if (scenes.Any(scene => scene.path == ScenePath))
        {
            return;
        }

        EditorBuildSettings.scenes = scenes
            .Append(
                new EditorBuildSettingsScene(
                    ScenePath,
                    true))
            .ToArray();
    }
}
