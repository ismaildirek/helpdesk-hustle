using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BossIntroDialogueBuilder
{
    private const string ScenePath =
        "Assets/Scenes/YeniOfis.unity";
    private const string FontPath =
        "Assets/Art/Fonts/Kenney Mini Square.ttf";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TrySetup;
    }

    [MenuItem("Tools/Yeni Ofis/Setup Boss Intro Dialogue")]
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
        Scene officeScene = SceneManager.GetSceneByPath(ScenePath);
        bool alreadyLoaded =
            officeScene.IsValid() && officeScene.isLoaded;

        if (!alreadyLoaded)
        {
            officeScene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
        }

        try
        {
            SceneManager.SetActiveScene(officeScene);
            Build(officeScene);
            EditorSceneManager.MarkSceneDirty(officeScene);
            EditorSceneManager.SaveScene(officeScene);
        }
        finally
        {
            if (originalScene.IsValid() && originalScene.isLoaded)
                SceneManager.SetActiveScene(originalScene);

            if (!alreadyLoaded &&
                officeScene.IsValid() &&
                officeScene.isLoaded)
            {
                EditorSceneManager.CloseScene(officeScene, true);
            }
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Boss Intro",
                "The five-page office introduction is ready.",
                "OK");
        }
    }

    private static void Build(Scene scene)
    {
        GameObject dialogueObject = FindObject(
            scene,
            "patron_konuşma");
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        if (dialogueObject == null || font == null)
        {
            Debug.LogError(
                "Boss intro setup needs patron_konuşma and the UI font.");
            return;
        }

        TextMesh bodyText = EnsureText(
            dialogueObject.transform,
            "DialogueText",
            new Vector3(-12.6f, -0.8f, 0f),
            TextAnchor.UpperLeft,
            TextAlignment.Left,
            72,
            0.16f,
            1.05f,
            new Color32(18, 35, 54, 255),
            101,
            font);

        TextMesh progressText = EnsureText(
            dialogueObject.transform,
            "ProgressText",
            new Vector3(0f, -22f, 0f),
            TextAnchor.LowerCenter,
            TextAlignment.Center,
            68,
            0.13f,
            1f,
            new Color32(39, 73, 111, 255),
            102,
            font);

        BossIntroDialogue controller =
            dialogueObject.GetComponent<BossIntroDialogue>();
        if (controller == null)
        {
            controller =
                Undo.AddComponent<BossIntroDialogue>(dialogueObject);
        }

        controller.ConfigureEditor(
            bodyText,
            progressText,
            BossIntroDialogue.CreateDefaultPages());
        EditorUtility.SetDirty(controller);
    }

    private static TextMesh EnsureText(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        TextAnchor anchor,
        TextAlignment alignment,
        int fontSize,
        float characterSize,
        float lineSpacing,
        Color color,
        int sortingOrder,
        Font font)
    {
        Transform textTransform = EnsureChild(parent, objectName);
        textTransform.localPosition = localPosition;
        textTransform.localRotation = Quaternion.identity;
        textTransform.localScale = Vector3.one;

        TextMesh text = textTransform.GetComponent<TextMesh>();
        if (text == null)
            text = Undo.AddComponent<TextMesh>(textTransform.gameObject);

        text.font = font;
        text.fontSize = fontSize;
        text.characterSize = characterSize;
        text.lineSpacing = lineSpacing;
        text.anchor = anchor;
        text.alignment = alignment;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.richText = false;

        MeshRenderer renderer =
            textTransform.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.sortingOrder = sortingOrder;

        EditorUtility.SetDirty(text);
        EditorUtility.SetDirty(renderer);
        return text;
    }

    private static Transform EnsureChild(
        Transform parent,
        string childName)
    {
        Transform child = parent
            .Cast<Transform>()
            .FirstOrDefault(item => item.name == childName);
        if (child != null)
            return child;

        GameObject childObject = new(childName);
        Undo.RegisterCreatedObjectUndo(
            childObject,
            $"Create {childName}");
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static GameObject FindObject(
        Scene scene,
        string objectName)
    {
        return scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item =>
                item.name.Equals(
                    objectName,
                    StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }
}
