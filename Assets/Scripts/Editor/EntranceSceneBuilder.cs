using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EntranceSceneBuilder
{
    private const string ScenePath =
        "Assets/Scenes/Giris_Ekran.unity";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TrySetup;
    }

    [MenuItem("Tools/Scenes/Setup Entrance Screen")]
    public static void SetupFromMenu()
    {
        Setup(true);
    }

    private static void TrySetup()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            Setup(false);
    }

    private static void Setup(bool showDialog)
    {
        Scene originalScene = SceneManager.GetActiveScene();
        Scene entranceScene = SceneManager.GetSceneByPath(ScenePath);
        bool alreadyLoaded =
            entranceScene.IsValid() && entranceScene.isLoaded;

        if (!alreadyLoaded)
        {
            entranceScene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
        }

        try
        {
            SceneManager.SetActiveScene(entranceScene);
            GameObject playButton = FindObject(
                entranceScene,
                "play_button");

            if (playButton == null)
            {
                Debug.LogError(
                    "Entrance setup could not find play_button.");
                return;
            }

            if (playButton.GetComponent<EntrancePlayButton>() == null)
                Undo.AddComponent<EntrancePlayButton>(playButton);

            EditorSceneManager.MarkSceneDirty(entranceScene);
            EditorSceneManager.SaveScene(entranceScene);
            EnsureEntranceIsFirstBuildScene();
        }
        finally
        {
            if (originalScene.IsValid() && originalScene.isLoaded)
                SceneManager.SetActiveScene(originalScene);

            if (!alreadyLoaded &&
                entranceScene.IsValid() &&
                entranceScene.isLoaded)
            {
                EditorSceneManager.CloseScene(entranceScene, true);
            }
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Giriş Ekranı",
                "Play düğmesi ve YeniOfis geçişi hazır.",
                "Tamam");
        }
    }

    private static void EnsureEntranceIsFirstBuildScene()
    {
        EditorBuildSettingsScene[] current =
            EditorBuildSettings.scenes;
        EditorBuildSettingsScene entrance =
            current.FirstOrDefault(scene => scene.path == ScenePath) ??
            new EditorBuildSettingsScene(ScenePath, true);
        entrance.enabled = true;

        EditorBuildSettings.scenes = new[] { entrance }
            .Concat(current.Where(scene => scene.path != ScenePath))
            .ToArray();
    }

    private static GameObject FindObject(
        Scene scene,
        string objectName)
    {
        return scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(transform =>
                transform.name.Equals(
                    objectName,
                    StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }
}
