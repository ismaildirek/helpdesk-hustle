using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BrokenPcRepairSceneBuilder
{
    private const string SceneName = "bozukkasa";
    private const string ScenePath =
        "Assets/Scenes/bozukkasa.unity";
    private const string RootName = "BrokenPcRepairSystem";
    private const string MarkerName = "BrokenPcRepair_v1";
    private const string ArtFolder =
        "Assets/Art/UI/kasa";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TryBuild;
    }

    [MenuItem("Tools/Mini Games/Build Broken PC Repair Scene")]
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
                    "Broken PC Repair",
                    "Open the bozukkasa scene first.",
                    "OK");
            }

            return;
        }

        Camera gameCamera = Camera.main;
        if (gameCamera == null)
        {
            Debug.LogError(
                "Broken PC Repair scene needs a Main Camera.");
            return;
        }

        Sprite brokenSprite =
            LoadSprite("pc_kasa_bozuk");
        Sprite repairedSprite =
            LoadSprite("pc_kasa_saglam");
        Sprite handSprite =
            LoadSprite("tokat_eli");
        Sprite backgroundSprite =
            LoadSprite("minioyun_arkaplan_sade");

        Sprite[] effectSprites =
        {
            LoadSprite("vurus_efektleri"),
            LoadSprite("az_patlama"),
            LoadSprite("patlama"),
            LoadSprite("yoğun_patlama")
        };

        if (brokenSprite == null ||
            repairedSprite == null ||
            handSprite == null ||
            effectSprites.Any(sprite => sprite == null))
        {
            Debug.LogError(
                "Broken PC Repair assets are missing from Assets/Art/UI/kasa.");
            return;
        }

        SpriteRenderer brokenCase =
            EnsureRenderer(
                scene,
                "pc_kasa_bozuk",
                brokenSprite);
        SpriteRenderer repairedCase =
            EnsureRenderer(
                scene,
                "pc_kasa_saglam",
                repairedSprite);
        SpriteRenderer hand =
            EnsureRenderer(
                scene,
                "tokat_eli",
                handSprite);
        SpriteRenderer hitEffect =
            EnsureRenderer(
                scene,
                "hit_effect",
                effectSprites[0]);

        bool brokenWasPlaced =
            brokenCase.transform.localScale != Vector3.one ||
            brokenCase.transform.position != Vector3.zero;

        if (!brokenWasPlaced)
        {
            FitSpriteHeight(
                brokenCase,
                gameCamera.orthographicSize * 1.05f);
            brokenCase.transform.position =
                new Vector3(
                    gameCamera.transform.position.x,
                    gameCamera.transform.position.y - 0.2f,
                    0f);
        }

        repairedCase.transform.SetPositionAndRotation(
            brokenCase.transform.position,
            brokenCase.transform.rotation *
            Quaternion.Euler(0f, 180f, 0f));

        float repairedScale =
            brokenCase.bounds.size.y /
            repairedSprite.bounds.size.y;
        repairedCase.transform.localScale =
            Vector3.one * repairedScale;

        bool handWasPlaced =
            hand.transform.localScale != Vector3.one ||
            hand.transform.position != Vector3.zero;

        if (!handWasPlaced)
        {
            FitSpriteWidth(hand, 3.0f);
            hand.transform.position =
                new Vector3(
                    brokenCase.bounds.max.x + 1.15f,
                    brokenCase.bounds.min.y + 0.65f,
                    -0.5f);
        }

        FitSpriteWidth(hitEffect, 2.2f);
        hitEffect.transform.position =
            new Vector3(
                brokenCase.bounds.center.x + 0.4f,
                brokenCase.bounds.center.y + 0.4f,
                -1f);

        brokenCase.sortingOrder = 10;
        repairedCase.sortingOrder = 10;
        hand.sortingOrder = 20;
        hitEffect.sortingOrder = 30;

        brokenCase.gameObject.SetActive(true);
        repairedCase.gameObject.SetActive(true);
        hand.gameObject.SetActive(true);
        hitEffect.gameObject.SetActive(true);

        repairedCase.enabled = false;
        hitEffect.enabled = false;

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
            "Build Broken PC Repair");

        GameObject marker = new(MarkerName);
        marker.transform.SetParent(root.transform, false);

        BrokenPcRepairController controller =
            root.AddComponent<BrokenPcRepairController>();

        controller.Configure(
            brokenCase,
            repairedCase,
            hand,
            hitEffect,
            effectSprites);

        AddSceneToBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        Debug.Log(
            "Broken PC Repair ready: five hits, moving hand and escalating hit effects.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Broken PC Repair",
                "Broken PC Repair mini game is ready.",
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
            .Append(new EditorBuildSettingsScene(ScenePath, true))
            .ToArray();
    }
}
