using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VirusHuntSceneBuilder
{
    private const string SceneName = "vir\u00FCs";
    private const string ScenePath =
        "Assets/Scenes/vir\u00FCs.unity";
    private const string RootName = "VirusHuntSystem";
    private const string MarkerName = "VirusHunt_v1";
    private const string BackIconGuid =
        "b49b25308b432cc4b8ad026fd058d890";
    private const string BackgroundGuid =
        "48171e20ddf73724cbab46f1d6ec5842";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TryBuild;
    }

    [MenuItem("Tools/Mini Games/Build Virus Hunt Scene")]
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
                    "Virus Hunt",
                    "Open the vir\u00FCs scene first.",
                    "OK");
            }

            return;
        }

        Camera gameCamera = Camera.main;
        if (gameCamera == null)
        {
            Debug.LogError(
                "Virus Hunt scene needs a Main Camera.");
            return;
        }

        List<SpriteRenderer> placedVirusRenderers = scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<SpriteRenderer>(true))
            .Where(renderer =>
                renderer.sprite != null &&
                renderer.sprite.name.StartsWith(
                    "virus_",
                    StringComparison.OrdinalIgnoreCase))
            .GroupBy(renderer => renderer.sprite)
            .Select(group => group.First())
            .Take(4)
            .ToList();

        Sprite[] virusSprites = LoadVirusSprites();
        if (virusSprites.Length < 4)
        {
            Debug.LogError(
                "Virus Hunt needs the four virus_ sprites in Assets/Art/UI.");
            return;
        }

        Vector3[] virusScales = virusSprites
            .Select(sprite =>
            {
                SpriteRenderer placed =
                    placedVirusRenderers.FirstOrDefault(
                        renderer => renderer.sprite == sprite);

                return placed != null
                    ? placed.transform.lossyScale
                    : Vector3.one;
            })
            .ToArray();

        foreach (SpriteRenderer template in placedVirusRenderers)
        {
            template.gameObject.SetActive(false);
            EditorUtility.SetDirty(template.gameObject);
        }

        EnsureBackground(scene, gameCamera);
        EnsureBackIcon(scene, gameCamera);

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
        }

        GameObject root = new(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Virus Hunt");

        GameObject marker = new(MarkerName);
        marker.transform.SetParent(root.transform, false);

        VirusHuntController controller =
            root.AddComponent<VirusHuntController>();
        controller.Configure(virusSprites, virusScales);

        AddSceneToBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        Debug.Log(
            "Virus Hunt ready: 10 persistent viruses, 12 second timer, maximum 5 visible.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Virus Hunt",
                "Virus Hunt scene is ready.",
                "OK");
        }
    }

    private static Sprite[] LoadVirusSprites()
    {
        return AssetDatabase
            .FindAssets(
                "virus_ t:Sprite",
                new[] { "Assets/Art/UI" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .SelectMany(path =>
                AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>())
            .Where(sprite =>
                sprite.name.StartsWith(
                    "virus_",
                    StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .OrderBy(sprite => sprite.name)
            .Take(4)
            .ToArray();
    }

    private static void EnsureBackground(
        Scene scene,
        Camera gameCamera)
    {
        Sprite backgroundSprite =
            LoadSpriteByGuid(BackgroundGuid);

        if (backgroundSprite == null)
        {
            return;
        }

        SpriteRenderer background = scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<SpriteRenderer>(true))
            .FirstOrDefault(renderer =>
                renderer.sprite == backgroundSprite);

        if (background == null)
        {
            GameObject backgroundObject =
                new("bg_virus_hunter");
            background =
                backgroundObject.AddComponent<SpriteRenderer>();
            background.sprite = backgroundSprite;
            Undo.RegisterCreatedObjectUndo(
                backgroundObject,
                "Create Virus Hunt Background");
        }

        float worldHeight = gameCamera.orthographicSize * 2f;
        float worldWidth = worldHeight * gameCamera.aspect;
        Vector2 spriteSize = backgroundSprite.bounds.size;
        float scale = Mathf.Max(
            worldWidth / spriteSize.x,
            worldHeight / spriteSize.y);

        background.transform.position =
            new Vector3(
                gameCamera.transform.position.x,
                gameCamera.transform.position.y,
                2f);
        background.transform.localScale =
            new Vector3(scale, scale, 1f);
        background.sortingOrder = -100;
        background.gameObject.SetActive(true);
        EditorUtility.SetDirty(background);
    }

    private static void EnsureBackIcon(
        Scene scene,
        Camera gameCamera)
    {
        Sprite backSprite = LoadSpriteByGuid(BackIconGuid);
        if (backSprite == null)
        {
            Debug.LogError("geri_ikon sprite is missing.");
            return;
        }

        SpriteRenderer backIcon = scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<SpriteRenderer>(true))
            .FirstOrDefault(renderer =>
                renderer.sprite == backSprite);

        if (backIcon == null)
        {
            GameObject backObject = new("geri_ikon");
            backIcon = backObject.AddComponent<SpriteRenderer>();
            backIcon.sprite = backSprite;

            float desiredHeight = 0.65f;
            float scale =
                desiredHeight / backSprite.bounds.size.y;
            backObject.transform.localScale =
                new Vector3(scale, scale, scale);

            float halfHeight = gameCamera.orthographicSize;
            float halfWidth = halfHeight * gameCamera.aspect;
            backObject.transform.position = new Vector3(
                gameCamera.transform.position.x -
                halfWidth + 0.65f,
                gameCamera.transform.position.y +
                halfHeight - 0.65f,
                0f);

            Undo.RegisterCreatedObjectUndo(
                backObject,
                "Create Virus Hunt Back Icon");
        }

        backIcon.sortingOrder = 100;
        backIcon.gameObject.SetActive(true);

        SceneIconButton button =
            backIcon.GetComponent<SceneIconButton>();
        if (button == null)
        {
            button =
                Undo.AddComponent<SceneIconButton>(
                    backIcon.gameObject);
        }

        button.Configure("YeniOfis");
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(backIcon);
    }

    private static Sprite LoadSpriteByGuid(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        return AssetDatabase
            .LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault();
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] scenes =
            EditorBuildSettings.scenes;

        if (scenes.Any(item => item.path == ScenePath))
        {
            return;
        }

        EditorBuildSettings.scenes = scenes
            .Append(new EditorBuildSettingsScene(ScenePath, true))
            .ToArray();
    }
}
