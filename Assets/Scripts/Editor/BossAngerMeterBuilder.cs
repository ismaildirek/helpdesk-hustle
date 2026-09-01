using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BossAngerMeterBuilder
{
    private const string ScenePath = "Assets/Scenes/YeniOfis.unity";
    private const string AssetFolder = "Assets/Art/OfisYeni/Ui";

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TrySetup;
    }

    [MenuItem("Tools/Yeni Ofis/Setup Boss Anger Meter")]
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
        bool openedForSetup = !officeScene.IsValid() ||
                              !officeScene.isLoaded;

        if (openedForSetup)
        {
            officeScene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
        }

        try
        {
            SceneManager.SetActiveScene(officeScene);
            BuildMeter(officeScene);
            EditorSceneManager.MarkSceneDirty(officeScene);
            EditorSceneManager.SaveScene(officeScene);
        }
        finally
        {
            if (originalScene.IsValid() && originalScene.isLoaded)
                SceneManager.SetActiveScene(originalScene);

            if (openedForSetup)
                EditorSceneManager.CloseScene(officeScene, true);
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Boss Anger Meter",
                "Green, yellow and red anger states are ready.",
                "OK");
        }
    }

    private static void BuildMeter(Scene scene)
    {
        GameObject frame =
            FindObject(scene, "sinir_seviyesiYatay") ??
            FindObject(scene, "sinir_seviyeYatay");
        Sprite greenSprite = LoadSprite("yeşil_bar.png");
        Sprite yellowSprite = LoadSprite("sarı_bar.png");
        Sprite redSprite = LoadSprite("kırmızı_bar.png");

        if (frame == null || greenSprite == null ||
            yellowSprite == null || redSprite == null)
        {
            Debug.LogError(
                "Boss anger setup needs the horizontal frame and " +
                "all three coloured bar sprites.");
            return;
        }

        SpriteRenderer frameRenderer =
            frame.GetComponent<SpriteRenderer>();
        int sortingOrder = frameRenderer != null
            ? frameRenderer.sortingOrder + 1
            : 101;

        SpriteRenderer green = EnsureBar(
            scene,
            frame.transform,
            "yeşil_bar",
            greenSprite,
            sortingOrder);
        SpriteRenderer yellow = EnsureBar(
            scene,
            frame.transform,
            "sarı_bar",
            yellowSprite,
            sortingOrder);
        SpriteRenderer red = EnsureBar(
            scene,
            frame.transform,
            "kırmızı_bar",
            redSprite,
            sortingOrder);

        BossAngerMeter meter = frame.GetComponent<BossAngerMeter>();
        if (meter == null)
            meter = Undo.AddComponent<BossAngerMeter>(frame);

        meter.ConfigureEditor(green, yellow, red);
        EditorUtility.SetDirty(meter);
    }

    private static SpriteRenderer EnsureBar(
        Scene scene,
        Transform frame,
        string objectName,
        Sprite sprite,
        int sortingOrder)
    {
        GameObject barObject = FindObject(scene, objectName);
        if (barObject == null)
        {
            barObject = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(
                barObject,
                $"Create {objectName}");
            SceneManager.MoveGameObjectToScene(barObject, scene);
        }

        Undo.RecordObject(barObject.transform, $"Align {objectName}");
        barObject.transform.position = frame.position;
        barObject.transform.rotation = frame.rotation;
        barObject.transform.localScale = frame.localScale;
        barObject.SetActive(true);

        SpriteRenderer renderer =
            barObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = Undo.AddComponent<SpriteRenderer>(barObject);

        Undo.RecordObject(renderer, $"Configure {objectName}");
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = sortingOrder;
        renderer.enabled = false;
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(barObject.transform);
        return renderer;
    }

    private static Sprite LoadSprite(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(
            $"{AssetFolder}/{fileName}");
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
