using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneIconNavigationLinker
{
    private const string MainOfficeScene = "YeniOfis";
    private const string FloorsScene = "katlar";

    private const string MainOfficePath =
        "Assets/Scenes/YeniOfis.unity";
    private const string FloorsPath =
        "Assets/Scenes/katlar.unity";
    private const string CableGamePath =
        "Assets/Scenes/kablo_game.unity";
    private const string FileUploadPath =
        "Assets/Scenes/Dosya_Y\u00FCkle.unity";
    private const string BrokenPcPath =
        "Assets/Scenes/bozukkasa.unity";
    private const string BrokenMonitorPath =
        "Assets/Scenes/bozukmonit\u00F6r.unity";
    private const string EmailGamePath =
        "Assets/Scenes/e_posta.unity";
    private const string PasswordGamePath =
        "Assets/Scenes/pasword_game.unity";
    private const string PopupAdsPath =
        "Assets/Scenes/popup_ads.unity";

    private const string ExitIconGuid =
        "ffeaa0ea5e6aad4478e4fed9eceb04b0";
    private const string HomeIconGuid =
        "11eec0a1738ee284f8055dd688de28e4";
    private const string BackIconGuid =
        "b49b25308b432cc4b8ad026fd058d890";

    private const string SetupVersion = "SceneIconNavigation_v4";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TrySetup;
    }

    private static void OnPlayModeStateChanged(
        PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += TrySetup;
        }
    }

    [MenuItem("Tools/Scene Navigation/Connect Scene Icons")]
    public static void SetupFromMenu()
    {
        SetupAllScenes(true);
    }

    private static void TrySetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (SessionState.GetBool(SetupVersion, false))
        {
            return;
        }

        SetupAllScenes(false);
        SessionState.SetBool(SetupVersion, true);
    }

    private static void SetupAllScenes(bool showDialog)
    {
        Scene originalActiveScene = SceneManager.GetActiveScene();

        bool success =
            EditScene(FloorsPath, scene =>
                ConfigureIcon(
                    scene,
                    ExitIconGuid,
                    MainOfficeScene)) &&
            EditScene(CableGamePath, scene =>
                ConfigureIcon(
                    scene,
                    BackIconGuid,
                    FloorsScene)) &&
            EditScene(FileUploadPath, scene =>
                ConfigureIcon(
                    scene,
                    BackIconGuid,
                    FloorsScene)) &&
            EditScene(BrokenPcPath, scene =>
                ConfigureIcon(
                    scene,
                    BackIconGuid,
                    FloorsScene)) &&
            EditScene(BrokenMonitorPath, scene =>
                ConfigureIcon(
                    scene,
                    BackIconGuid,
                    FloorsScene)) &&
            EditScene(EmailGamePath, scene =>
                ConfigureIcon(
                    scene,
                    BackIconGuid,
                    FloorsScene,
                    true)) &&
            EditScene(PasswordGamePath, scene =>
                ConfigureIcon(
                    scene,
                    BackIconGuid,
                    FloorsScene,
                    true)) &&
            EditScene(PopupAdsPath, scene =>
                ConfigureIcon(
                    scene,
                    BackIconGuid,
                    FloorsScene,
                    true)) &&
            EditScene(MainOfficePath, scene =>
                ConfigureIcon(scene, HomeIconGuid, FloorsScene));

        if (originalActiveScene.IsValid() &&
            originalActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(originalActiveScene);
        }

        if (success)
        {
            Debug.Log(
                "Scene icons connected: floors exit, mini-game back buttons and YeniOfis home icon.");
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Scene Navigation",
                success
                    ? "Scene icons are connected."
                    : "Some scene icons could not be found. Check the Console.",
                "OK");
        }
    }

    private static bool EditScene(
        string scenePath,
        Func<Scene, bool> editAction)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;

        if (!wasLoaded)
        {
            scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Additive);
        }

        bool changed = editAction(scene);
        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (!wasLoaded)
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        return changed;
    }

    private static bool ConfigureIcon(
        Scene scene,
        string spriteGuid,
        string targetScene,
        bool createIfMissing = false)
    {
        SpriteRenderer icon = FindRendererBySpriteGuid(
            scene,
            spriteGuid);

        if (icon == null)
        {
            icon = scene
                .GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<SpriteRenderer>(true))
                .FirstOrDefault(renderer =>
                    renderer.name.StartsWith(
                        "geri_ikon",
                        StringComparison.OrdinalIgnoreCase));
        }

        if (icon == null)
        {
            if (createIfMissing)
                icon = CreateNavigationIcon(scene, spriteGuid);
        }

        if (icon == null)
        {
            Debug.LogError(
                $"Navigation icon was not found in {scene.path}.");
            return false;
        }

        SceneIconButton button =
            icon.GetComponent<SceneIconButton>();

        if (button == null)
        {
            button = Undo.AddComponent<SceneIconButton>(
                icon.gameObject);
        }

        button.Configure(targetScene);
        EditorUtility.SetDirty(button);
        return true;
    }

    private static SpriteRenderer CreateNavigationIcon(
        Scene scene,
        string spriteGuid)
    {
        string spritePath = AssetDatabase.GUIDToAssetPath(spriteGuid);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath) ??
            AssetDatabase.LoadAllAssetsAtPath(spritePath)
                .OfType<Sprite>()
                .FirstOrDefault();
        if (sprite == null)
            return null;

        Camera camera = scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<Camera>(true))
            .FirstOrDefault(candidate => candidate.orthographic);
        if (camera == null)
            return null;

        GameObject iconObject = new("geri_ikon");
        Undo.RegisterCreatedObjectUndo(
            iconObject,
            "Create mini-game back icon");
        SceneManager.MoveGameObjectToScene(iconObject, scene);

        SpriteRenderer renderer =
            Undo.AddComponent<SpriteRenderer>(iconObject);
        renderer.sprite = sprite;
        renderer.sortingOrder = 100;

        float renderedHeight = Mathf.Max(
            0.75f,
            camera.orthographicSize * 0.135f);
        float scale = renderedHeight /
            Mathf.Max(0.001f, sprite.bounds.size.y);
        iconObject.transform.localScale = new Vector3(scale, scale, 1f);

        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * camera.aspect;
        float padding = renderedHeight * 0.75f;
        Vector3 desiredCenter = new(
            camera.transform.position.x - halfWidth + padding,
            camera.transform.position.y + halfHeight - padding,
            -3f);
        iconObject.transform.position +=
            desiredCenter - renderer.bounds.center;
        iconObject.transform.position = new Vector3(
            iconObject.transform.position.x,
            iconObject.transform.position.y,
            -3f);

        EditorUtility.SetDirty(renderer);
        return renderer;
    }

    private static SpriteRenderer FindRendererBySpriteGuid(
        Scene scene,
        string spriteGuid)
    {
        string spritePath =
            AssetDatabase.GUIDToAssetPath(spriteGuid);

        return scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<SpriteRenderer>(true))
            .FirstOrDefault(renderer =>
                renderer.sprite != null &&
                AssetDatabase.GetAssetPath(renderer.sprite)
                    .Equals(
                        spritePath,
                        StringComparison.OrdinalIgnoreCase));
    }
}
