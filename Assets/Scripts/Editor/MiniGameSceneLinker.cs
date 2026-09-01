using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MiniGameSceneLinker
{
    private const string EntranceScenePath =
        "Assets/Scenes/Giris_Ekran.unity";
    private const string FloorsScenePath = "Assets/Scenes/katlar.unity";
    private const string CableScenePath = "Assets/Scenes/kablo_game.unity";
    private const string FileUploadScenePath =
        "Assets/Scenes/Dosya_Y\u00FCkle.unity";
    private const string VirusScenePath =
        "Assets/Scenes/vir\u00FCs.unity";
    private const string BrokenPcScenePath =
        "Assets/Scenes/bozukkasa.unity";
    private const string BrokenMonitorScenePath =
        "Assets/Scenes/bozukmonit\u00F6r.unity";
    private const string MainOfficeScenePath = "Assets/Scenes/YeniOfis.unity";
    private const string CableButtonName = "bt_1.1";
    private const string FileUploadButtonName = "bt_1.2";
    private const string VirusButtonName = "bt_1.3";
    private const string BrokenPcButtonName = "bt_1.4";
    private const string BrokenMonitorButtonName = "bt_1.5";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TryConfigure;
    }

    [MenuItem("Tools/Mini Games/Connect Floor Mini Games")]
    public static void ConfigureFromMenu()
    {
        ConfigureButton(true);
    }

    private static void TryConfigure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        ConfigureButton(false);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += TryConfigure;
        }
    }

    private static void ConfigureButton(bool showDialog)
    {
        Scene floorsScene = SceneManager.GetSceneByPath(FloorsScenePath);
        bool wasAlreadyLoaded = floorsScene.IsValid() && floorsScene.isLoaded;

        if (!wasAlreadyLoaded)
        {
            floorsScene = EditorSceneManager.OpenScene(
                FloorsScenePath,
                OpenSceneMode.Additive);
        }

        GameObject buttonObject = floorsScene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .FirstOrDefault(item => item.name == CableButtonName);

        if (buttonObject == null)
        {
            Debug.LogError("Mini Game linker could not find bt_1.1.");
            CloseIfNeeded(floorsScene, wasAlreadyLoaded);
            return;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("bt_1.1 does not have a Button component.");
            CloseIfNeeded(floorsScene, wasAlreadyLoaded);
            return;
        }

        MiniGameLauncher launcher =
            buttonObject.GetComponent<MiniGameLauncher>();

        if (launcher == null)
        {
            launcher = Undo.AddComponent<MiniGameLauncher>(buttonObject);
        }

        for (int index = button.onClick.GetPersistentEventCount() - 1;
             index >= 0;
             index--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, index);
        }

        UnityEventTools.AddPersistentListener(
            button.onClick,
            launcher.OpenCableGame);

        GameObject fileUploadButtonObject = floorsScene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .FirstOrDefault(item =>
                item.name.StartsWith(FileUploadButtonName));

        if (fileUploadButtonObject == null)
        {
            Debug.LogError("Mini Game linker could not find bt_1.2.");
            CloseIfNeeded(floorsScene, wasAlreadyLoaded);
            return;
        }

        fileUploadButtonObject.name = FileUploadButtonName;

        Button fileUploadButton =
            fileUploadButtonObject.GetComponent<Button>();

        if (fileUploadButton == null)
        {
            Debug.LogError("bt_1.2 does not have a Button component.");
            CloseIfNeeded(floorsScene, wasAlreadyLoaded);
            return;
        }

        MiniGameLauncher fileUploadLauncher =
            fileUploadButtonObject.GetComponent<MiniGameLauncher>();

        if (fileUploadLauncher == null)
        {
            fileUploadLauncher =
                Undo.AddComponent<MiniGameLauncher>(
                    fileUploadButtonObject);
        }

        for (int index =
                 fileUploadButton.onClick.GetPersistentEventCount() - 1;
             index >= 0;
             index--)
        {
            UnityEventTools.RemovePersistentListener(
                fileUploadButton.onClick,
                index);
        }

        UnityEventTools.AddPersistentListener(
            fileUploadButton.onClick,
            fileUploadLauncher.OpenFileUpload);

        GameObject virusButtonObject = floorsScene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .FirstOrDefault(item =>
                item.name.StartsWith(VirusButtonName));

        if (virusButtonObject == null)
        {
            Debug.LogError("Mini Game linker could not find bt_1.3.");
            CloseIfNeeded(floorsScene, wasAlreadyLoaded);
            return;
        }

        virusButtonObject.name = VirusButtonName;

        Button virusButton =
            virusButtonObject.GetComponent<Button>();

        if (virusButton == null)
        {
            Debug.LogError("bt_1.3 does not have a Button component.");
            CloseIfNeeded(floorsScene, wasAlreadyLoaded);
            return;
        }

        MiniGameLauncher virusLauncher =
            virusButtonObject.GetComponent<MiniGameLauncher>();

        if (virusLauncher == null)
        {
            virusLauncher =
                Undo.AddComponent<MiniGameLauncher>(
                    virusButtonObject);
        }

        for (int index =
                 virusButton.onClick.GetPersistentEventCount() - 1;
             index >= 0;
             index--)
        {
            UnityEventTools.RemovePersistentListener(
                virusButton.onClick,
                index);
        }

        UnityEventTools.AddPersistentListener(
            virusButton.onClick,
            virusLauncher.OpenVirusGame);

        GameObject brokenPcButtonObject = floorsScene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .FirstOrDefault(item =>
                item.name.StartsWith(BrokenPcButtonName));

        if (brokenPcButtonObject == null)
        {
            Debug.LogError("Mini Game linker could not find bt_1.4.");
            CloseIfNeeded(floorsScene, wasAlreadyLoaded);
            return;
        }

        brokenPcButtonObject.name = BrokenPcButtonName;

        Button brokenPcButton =
            brokenPcButtonObject.GetComponent<Button>();

        if (brokenPcButton == null)
        {
            Debug.LogError("bt_1.4 does not have a Button component.");
            CloseIfNeeded(floorsScene, wasAlreadyLoaded);
            return;
        }

        MiniGameLauncher brokenPcLauncher =
            brokenPcButtonObject.GetComponent<MiniGameLauncher>();

        if (brokenPcLauncher == null)
        {
            brokenPcLauncher =
                Undo.AddComponent<MiniGameLauncher>(
                    brokenPcButtonObject);
        }

        for (int index =
                 brokenPcButton.onClick.GetPersistentEventCount() - 1;
             index >= 0;
             index--)
        {
            UnityEventTools.RemovePersistentListener(
                brokenPcButton.onClick,
                index);
        }

        UnityEventTools.AddPersistentListener(
            brokenPcButton.onClick,
            brokenPcLauncher.OpenBrokenPcRepair);

        GameObject brokenMonitorButtonObject = floorsScene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .FirstOrDefault(item =>
                item.name.StartsWith(BrokenMonitorButtonName));

        if (brokenMonitorButtonObject == null)
        {
            Debug.LogError("Mini Game linker could not find bt_1.5.");
            CloseIfNeeded(floorsScene, wasAlreadyLoaded);
            return;
        }

        brokenMonitorButtonObject.name =
            BrokenMonitorButtonName;

        Button brokenMonitorButton =
            brokenMonitorButtonObject.GetComponent<Button>();

        if (brokenMonitorButton == null)
        {
            Debug.LogError("bt_1.5 does not have a Button component.");
            CloseIfNeeded(floorsScene, wasAlreadyLoaded);
            return;
        }

        MiniGameLauncher brokenMonitorLauncher =
            brokenMonitorButtonObject
                .GetComponent<MiniGameLauncher>();

        if (brokenMonitorLauncher == null)
        {
            brokenMonitorLauncher =
                Undo.AddComponent<MiniGameLauncher>(
                    brokenMonitorButtonObject);
        }

        for (int index =
                 brokenMonitorButton.onClick
                     .GetPersistentEventCount() - 1;
             index >= 0;
             index--)
        {
            UnityEventTools.RemovePersistentListener(
                brokenMonitorButton.onClick,
                index);
        }

        UnityEventTools.AddPersistentListener(
            brokenMonitorButton.onClick,
            brokenMonitorLauncher.OpenBrokenMonitorRepair);

        EnsureScenesInBuildSettings();
        EditorSceneManager.MarkSceneDirty(floorsScene);
        EditorSceneManager.SaveScene(floorsScene);
        CloseIfNeeded(floorsScene, wasAlreadyLoaded);

        Debug.Log(
            "Floor mini-game buttons bt_1.1 through bt_1.5 are connected.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Mini Game",
                "bt_1.1 through bt_1.5 are connected.",
                "OK");
        }
    }

    private static void EnsureScenesInBuildSettings()
    {
        string[] requiredPaths =
        {
            EntranceScenePath,
            MainOfficeScenePath,
            FloorsScenePath,
            CableScenePath,
            FileUploadScenePath,
            VirusScenePath,
            BrokenPcScenePath,
            BrokenMonitorScenePath
        };

        EditorBuildSettingsScene[] existing =
            EditorBuildSettings.scenes;

        EditorBuildSettings.scenes = requiredPaths
            .Select(path =>
            {
                EditorBuildSettingsScene current =
                    existing.FirstOrDefault(scene => scene.path == path);

                return current ?? new EditorBuildSettingsScene(path, true);
            })
            .Concat(existing.Where(scene =>
                !requiredPaths.Contains(scene.path)))
            .ToArray();
    }

    private static void CloseIfNeeded(
        Scene scene,
        bool wasAlreadyLoaded)
    {
        if (!wasAlreadyLoaded && scene.IsValid() && scene.isLoaded)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
