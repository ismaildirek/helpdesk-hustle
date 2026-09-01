using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ModemSceneBuilder
{
    private const string SceneName = "modem";
    private const string ScenePath = "Assets/Scenes/modem.unity";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TrySetup;
    }

    [MenuItem("Tools/Mini Games/Setup Modem Game")]
    public static void SetupFromMenu()
    {
        Setup(true);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += TrySetup;
    }

    private static void TrySetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == SceneName)
            Setup(false);
    }

    private static void Setup(bool showDialog)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SceneName)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Modem Mini Game",
                    "Önce modem sahnesini aç.",
                    "Tamam");
            }
            return;
        }

        SpriteRenderer cable = FindRenderer(scene, "kablo");
        SpriteRenderer modem = FindRenderer(scene, "modem");
        SpriteRenderer background = FindRenderer(scene, "modem_arkaplan");
        Camera camera = Camera.main;

        if (cable == null || modem == null || background == null || camera == null)
        {
            Debug.LogError(
                "Modem setup needs kablo, modem, modem_arkaplan and Main Camera.");
            return;
        }

        cable.name = "kablo";
        cable.sortingOrder = 2;
        modem.sortingOrder = 1;
        background.sortingOrder = 0;

        ModemCableMiniGame controller =
            UnityEngine.Object.FindFirstObjectByType<ModemCableMiniGame>();
        if (controller == null)
        {
            GameObject controllerObject =
                new GameObject("ModemMiniGameController");
            controller = Undo.AddComponent<ModemCableMiniGame>(
                controllerObject);
        }

        controller.Configure(cable, modem, camera);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(cable);
        EditorUtility.SetDirty(modem);
        EditorUtility.SetDirty(background);

        EnsureSceneInBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log(
            "Modem mini game configured: moving cable targets the blue port.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Modem Mini Game",
                "Modem mini oyunu hazır.",
                "Tamam");
        }
    }

    private static SpriteRenderer FindRenderer(
        Scene scene,
        string objectName)
    {
        return scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<SpriteRenderer>(true))
            .FirstOrDefault(renderer =>
                renderer.name.Trim().Equals(
                    objectName,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureSceneInBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(scene => scene.path == ScenePath))
            return;

        EditorBuildSettings.scenes = scenes
            .Concat(new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            })
            .ToArray();
    }
}
