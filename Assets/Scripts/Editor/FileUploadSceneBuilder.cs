using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FileUploadSceneBuilder
{
    private const string SceneName = "Dosya_Yükle";
    private const string ScenePath = "Assets/Scenes/Dosya_Yükle.unity";
    private const string RootName = "FileUploadSystem";
    private const string MarkerName = "FileUpload_v2";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TryBuild;
    }

    [MenuItem("Tools/Mini Games/Build File Upload Scene")]
    public static void BuildFromMenu()
    {
        Build(true);
    }

    private static void TryBuild()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SceneName)
        {
            return;
        }

        GameObject root = GameObject.Find(RootName);
        bool isCurrent =
            root != null &&
            root.transform.Find(MarkerName) != null;

        if (isCurrent)
        {
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        Build(false);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
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
                    "File Upload",
                    "Open the Dosya_Yükle scene first.",
                    "OK");
            }

            return;
        }

        SpriteRenderer folderOpen = FindRenderer(scene, "folder_open");
        SpriteRenderer folderClosed = FindRenderer(scene, "folder_closed");
        SpriteRenderer paperFile = FindRenderer(scene, "paper_file");
        SpriteRenderer progressEmpty =
            FindRenderer(scene, "progress_bar_empty");
        SpriteRenderer uploadButton =
            FindUploadButton(scene);

        SpriteRenderer[] required =
        {
            folderOpen,
            folderClosed,
            paperFile,
            progressEmpty,
            uploadButton
        };

        if (required.Any(item => item == null))
        {
            Debug.LogError(
                "File Upload setup needs folder_open, folder_closed, paper_file, progress_bar_empty and upload_button in the active scene.");
            return;
        }

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
        }

        GameObject root = new(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build File Upload");

        GameObject marker = new(MarkerName);
        marker.transform.SetParent(root.transform, false);

        SpriteRenderer progressFull =
            FindRenderer(scene, "progress_bar_full");

        if (progressFull == null)
        {
            GameObject fullObject = new("progress_bar_full");
            Undo.RegisterCreatedObjectUndo(
                fullObject,
                "Create Full Progress Bar");

            progressFull =
                fullObject.AddComponent<SpriteRenderer>();
            progressFull.sprite =
                LoadTaskSprite("progress_bar_full");
            progressFull.sortingLayerID =
                progressEmpty.sortingLayerID;
            progressFull.sortingOrder =
                progressEmpty.sortingOrder + 1;

            fullObject.transform.SetPositionAndRotation(
                progressEmpty.transform.position,
                progressEmpty.transform.rotation);
            fullObject.transform.localScale =
                progressEmpty.transform.lossyScale;
        }

        FileUploadController controller =
            root.AddComponent<FileUploadController>();

        controller.Configure(
            folderOpen,
            folderClosed,
            paperFile,
            progressEmpty,
            progressFull,
            uploadButton);

        AddSceneToBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        Debug.Log(
            "File Upload scene ready: upload button, two moving papers and filling progress bar.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "File Upload",
                "File upload interaction is ready.",
                "OK");
        }
    }

    private static SpriteRenderer FindRenderer(
        Scene scene,
        string assetName)
    {
        return scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<SpriteRenderer>(true))
            .FirstOrDefault(renderer =>
                renderer.gameObject.name.Equals(
                    assetName,
                    StringComparison.OrdinalIgnoreCase) ||
                (renderer.sprite != null &&
                 renderer.sprite.name.Contains(
                     assetName,
                     StringComparison.OrdinalIgnoreCase)));
    }

    private static SpriteRenderer FindUploadButton(Scene scene)
    {
        return scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<SpriteRenderer>(true))
            .Where(renderer =>
                renderer.gameObject.name.Contains(
                    "upload_button",
                    StringComparison.OrdinalIgnoreCase) ||
                (renderer.sprite != null &&
                 renderer.sprite.name.Contains(
                     "upload_button",
                     StringComparison.OrdinalIgnoreCase)))
            .OrderBy(renderer =>
                Mathf.Abs(
                    renderer.bounds.size.x *
                    renderer.bounds.size.y))
            .FirstOrDefault();
    }

    private static Sprite LoadTaskSprite(string assetName)
    {
        string guid = AssetDatabase
            .FindAssets($"{assetName} t:Sprite")
            .FirstOrDefault();

        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError($"File Upload sprite is missing: {assetName}");
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(
            AssetDatabase.GUIDToAssetPath(guid));
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
