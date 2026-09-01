using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NoTaskRoomFeedbackBuilder
{
    private const string ScenePath =
        "Assets/Scenes/katlar.unity";
    private const string IconFolder =
        "Assets/Art/UI/g\u00F6rev_ikon";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TrySetup;
    }

    [MenuItem("Tools/Mini Games/Setup No Task Room Feedback")]
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
        Scene floorsScene = SceneManager.GetSceneByPath(ScenePath);
        bool alreadyLoaded =
            floorsScene.IsValid() && floorsScene.isLoaded;

        if (!alreadyLoaded)
        {
            floorsScene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
        }

        try
        {
            SceneManager.SetActiveScene(floorsScene);
            BuildFeedback(floorsScene);
            EditorSceneManager.MarkSceneDirty(floorsScene);
            EditorSceneManager.SaveScene(floorsScene);
        }
        finally
        {
            if (originalScene.IsValid() && originalScene.isLoaded)
                SceneManager.SetActiveScene(originalScene);

            if (!alreadyLoaded &&
                floorsScene.IsValid() &&
                floorsScene.isLoaded)
            {
                EditorSceneManager.CloseScene(floorsScene, true);
            }
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Görevsiz Oda",
                "Üç oda görseli ve 2 saniyelik dönüş hazır.",
                "Tamam");
        }
    }

    private static void BuildFeedback(Scene scene)
    {
        Canvas mainCanvas = scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<Canvas>(true))
            .FirstOrDefault(canvas => canvas.isRootCanvas);
        GameManager roomManager = scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<GameManager>(true))
            .FirstOrDefault();

        if (mainCanvas == null || roomManager == null)
        {
            Debug.LogError(
                "No-task room setup needs the katlar Canvas and GameManager.");
            return;
        }

        RectTransform overlay = EnsureRectChild(
            mainCanvas.transform,
            "NoTaskRoomOverlay");
        StretchToParent(overlay);

        Canvas overlayCanvas =
            overlay.GetComponent<Canvas>();
        if (overlayCanvas == null)
            overlayCanvas = Undo.AddComponent<Canvas>(
                overlay.gameObject);
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 100;

        if (overlay.GetComponent<GraphicRaycaster>() == null)
            Undo.AddComponent<GraphicRaycaster>(overlay.gameObject);

        CanvasGroup canvasGroup = overlay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = Undo.AddComponent<CanvasGroup>(overlay.gameObject);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Image backdrop = overlay.GetComponent<Image>();
        if (backdrop == null)
            backdrop = Undo.AddComponent<Image>(overlay.gameObject);
        backdrop.color = new Color(0f, 0f, 0f, 0.94f);
        backdrop.raycastTarget = true;

        RectTransform imageTransform = EnsureRectChild(
            overlay,
            "RoomFeedbackImage");
        StretchToParent(imageTransform);

        Image feedbackImage =
            imageTransform.GetComponent<Image>();
        if (feedbackImage == null)
        {
            feedbackImage = Undo.AddComponent<Image>(
                imageTransform.gameObject);
        }
        feedbackImage.color = Color.white;
        feedbackImage.preserveAspect = true;
        feedbackImage.raycastTarget = false;

        Sprite[] sprites =
        {
            LoadSprite("oda_dolu_1_bos"),
            LoadSprite("oda_dolu_2_getout"),
            LoadSprite("oda_dolu_3_mesgul")
        };

        NoTaskRoomFeedback feedback =
            roomManager.GetComponent<NoTaskRoomFeedback>();
        if (feedback == null)
        {
            feedback = Undo.AddComponent<NoTaskRoomFeedback>(
                roomManager.gameObject);
        }

        feedback.ConfigureEditor(
            overlay.gameObject,
            feedbackImage,
            sprites);
        roomManager.ConfigureNoTaskFeedback(feedback);

        MiniGameLauncher[] launchers = scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<MiniGameLauncher>(true))
            .ToArray();
        foreach (MiniGameLauncher launcher in launchers)
        {
            launcher.ConfigureNoTaskFeedback(feedback);
            EditorUtility.SetDirty(launcher);
        }

        overlay.gameObject.SetActive(false);
        EditorUtility.SetDirty(feedback);
        EditorUtility.SetDirty(roomManager);
        EditorUtility.SetDirty(overlayCanvas);
        EditorUtility.SetDirty(canvasGroup);
        EditorUtility.SetDirty(backdrop);
        EditorUtility.SetDirty(feedbackImage);
    }

    private static Sprite LoadSprite(string assetName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            $"{IconFolder}/{assetName}.png");
        if (sprite == null)
        {
            Debug.LogError(
                $"No-task room sprite could not be loaded: {assetName}");
        }

        return sprite;
    }

    private static RectTransform EnsureRectChild(
        Transform parent,
        string childName)
    {
        Transform existing = parent
            .Cast<Transform>()
            .FirstOrDefault(child => child.name == childName);
        if (existing is RectTransform existingRect)
            return existingRect;

        GameObject childObject = new(
            childName,
            typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(
            childObject,
            $"Create {childName}");
        RectTransform rect =
            childObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
