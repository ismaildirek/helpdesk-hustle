using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CableGameSceneBuilder
{
    private const string SceneName = "kablo_game";
    private const string RootName = "CableGame";
    private const string SetupMarkerName =
        "NamedHeads_6Cables_v6";

    private static readonly string[] Colors =
    {
        "Blue",
        "Green",
        "Orange",
        "Purple",
        "Yellow",
        "Red"
    };

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TryAutoBuild;
    }

    [MenuItem("Tools/Cable Game/Rebuild Named Cable Game")]
    public static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void TryAutoBuild()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SceneName ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        GameObject root = GameObject.Find(RootName);
        if (root != null &&
            root.transform.Find(SetupMarkerName) != null)
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
            EditorApplication.delayCall += TryAutoBuild;
        }
    }

    private static void Build(bool forcedByMenu)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SceneName)
        {
            if (forcedByMenu)
            {
                EditorUtility.DisplayDialog(
                    "Cable Game",
                    "Open the kablo_game scene first.",
                    "OK");
            }

            return;
        }

        SpriteRenderer[] leftHeads = Colors
            .Select(color =>
                FindRenderer(scene, $"wire_head{color}_left"))
            .ToArray();

        SpriteRenderer[] rightHeads = Colors
            .Select(color =>
                FindRenderer(scene, $"wire_head{color}_right"))
            .ToArray();

        if (leftHeads.Any(item => item == null) ||
            rightHeads.Any(item => item == null))
        {
            Debug.LogError(
                "Cable Game needs left and right cable heads for Blue, Green, Orange, Purple, Yellow and Red.");
            return;
        }

        foreach (SpriteRenderer head in
                 leftHeads.Concat(rightHeads))
        {
            head.gameObject.SetActive(true);
            head.enabled = true;
            head.sortingOrder = 10;
        }

        foreach (string color in Colors)
        {
            SpriteRenderer oldCableBody =
                FindRenderer(scene, color.ToLowerInvariant());

            if (oldCableBody != null)
            {
                oldCableBody.enabled = false;
            }
        }

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
        }

        GameObject root = new(RootName);
        Undo.RegisterCreatedObjectUndo(
            root,
            "Build Named Cable Game");

        GameObject marker = new(SetupMarkerName);
        marker.transform.SetParent(root.transform, false);

        CableGameManager manager =
            root.AddComponent<CableGameManager>();
        manager.Configure(Colors.Length);

        CableGameWorldController controller =
            root.AddComponent<CableGameWorldController>();
        controller.Configure(
            leftHeads,
            rightHeads,
            manager);

        AddSceneToBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        Debug.Log(
            "Cable Game v6 ready: six named color pairs, direct drag input and no collider dependency.");
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
                renderer.gameObject.name.Equals(
                    objectName,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static void AddSceneToBuildSettings()
    {
        string scenePath = SceneManager.GetActiveScene().path;
        EditorBuildSettingsScene[] scenes =
            EditorBuildSettings.scenes;

        if (scenes.Any(item => item.path == scenePath))
        {
            return;
        }

        EditorBuildSettings.scenes = scenes
            .Append(
                new EditorBuildSettingsScene(
                    scenePath,
                    true))
            .ToArray();
    }
}
