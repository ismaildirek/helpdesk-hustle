using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EmailTriageSceneInstaller
{
    private const string ScenePath = "Assets/Scenes/e_posta.unity";
    private const string BackgroundGuid = "5a4e9db50f85e654cae1ccea27beddee";
    private const string SafeEmailGuid = "e889fce61623b5c479a1ebd8b9b90107";
    private const string MaliciousEmailGuid = "43dded60047fec243b9244081d4d7ec1";
    private const string SafeButtonGuid = "a3cf158784be7e74fab5d92a61e7f1ee";
    private const string MaliciousButtonGuid = "c30ded89dd1b44345b691d89de8b16a2";
    private const string AlertIconGuid = "aa7bcdd95cf3d704fb186678f10db71b";
    private const string BackIconGuid = "b49b25308b432cc4b8ad026fd058d890";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TryInstall;
    }

    [MenuItem("Tools/Mini Games/Install E-Posta Mini Game")]
    public static void InstallFromMenu()
    {
        Install(true);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += TryInstall;
        }
    }

    private static void TryInstall()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Install(false);
    }

    private static void Install(bool showDialog)
    {
        Scene originalActiveScene = SceneManager.GetActiveScene();
        Scene emailScene = SceneManager.GetSceneByPath(ScenePath);
        bool wasLoaded = emailScene.IsValid() && emailScene.isLoaded;

        if (!wasLoaded)
        {
            emailScene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
        }

        SceneManager.SetActiveScene(emailScene);

        Camera gameCamera = emailScene
            .GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .FirstOrDefault(camera => camera.CompareTag("MainCamera"))
            ?? emailScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault();

        bool success = gameCamera != null;

        if (success)
        {
            EmailTriageRuntimeBootstrap bootstrap =
                gameCamera.GetComponent<EmailTriageRuntimeBootstrap>();

            if (bootstrap == null)
            {
                bootstrap = Undo.AddComponent<EmailTriageRuntimeBootstrap>(
                    gameCamera.gameObject);
            }

            bootstrap.Configure(
                LoadLargestSprite(BackgroundGuid),
                LoadLargestSprite(SafeEmailGuid),
                LoadLargestSprite(MaliciousEmailGuid),
                LoadLargestSprite(SafeButtonGuid),
                LoadLargestSprite(MaliciousButtonGuid),
                LoadLargestSprite(AlertIconGuid));

            // Materialize the mini game in Edit Mode as well. This keeps the
            // scene visible before Play and preserves any renderers the user
            // already positioned because the bootstrap reuses them by sprite.
            bootstrap.EnsureGameInScene(gameCamera);
            EnsureBackIcon(emailScene);

            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(emailScene);
            EditorSceneManager.SaveScene(emailScene);
            AddSceneToBuildSettings();
        }
        else
        {
            Debug.LogError("e_posta scene needs a Main Camera.");
        }

        if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(originalActiveScene);
        }

        if (!wasLoaded)
        {
            EditorSceneManager.CloseScene(emailScene, true);
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "E-Posta Mini Game",
                success
                    ? "E-posta mini game is connected. Existing sprite positions and scales will be preserved."
                    : "The e_posta scene needs a Main Camera.",
                "OK");
        }
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

    private static void EnsureBackIcon(Scene scene)
    {
        Sprite backSprite = LoadLargestSprite(BackIconGuid);
        if (backSprite == null)
        {
            Debug.LogError("E-posta back icon sprite could not be loaded.");
            return;
        }

        SpriteRenderer[] renderers = scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<SpriteRenderer>(true))
            .ToArray();

        SpriteRenderer backIcon = renderers.FirstOrDefault(renderer =>
            renderer.sprite == backSprite ||
            renderer.name.StartsWith(
                "geri_ikon",
                StringComparison.OrdinalIgnoreCase));

        if (backIcon == null)
        {
            SpriteRenderer background = renderers.FirstOrDefault(renderer =>
                renderer.sprite != null &&
                AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(renderer.sprite)) == BackgroundGuid);

            GameObject iconObject = new("geri_ikon");
            Undo.RegisterCreatedObjectUndo(iconObject, "Create e-posta back icon");
            backIcon = Undo.AddComponent<SpriteRenderer>(iconObject);
            backIcon.sprite = backSprite;
            backIcon.sortingOrder = 100;

            Bounds backgroundBounds = background != null
                ? background.bounds
                : new Bounds(Vector3.zero, new Vector3(29f, 52.25f, 1f));
            backIcon.transform.position = new Vector3(
                backgroundBounds.min.x + 2.4f,
                backgroundBounds.max.y - 2.4f,
                -3f);

            float spriteHeight = Mathf.Max(0.001f, backSprite.bounds.size.y);
            float scale = 3.5f / spriteHeight;
            backIcon.transform.localScale = new Vector3(scale, scale, 1f);
        }

        SceneIconButton button = backIcon.GetComponent<SceneIconButton>();
        if (button == null)
            button = Undo.AddComponent<SceneIconButton>(backIcon.gameObject);

        button.Configure("katlar");
        EditorUtility.SetDirty(backIcon);
        EditorUtility.SetDirty(button);
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
