using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TaskCardMissionBoardBuilder
{
    private const string ScenePath = "Assets/Scenes/YeniOfis.unity";
    private const string FontPath =
        "Assets/Art/Fonts/Kenney Mini Square.ttf";
    private const string IconFolder =
        "Assets/Art/UI/görev_ikon";
    private const string CardDecorationPath =
        "Assets/Art/UI/kulaklık_kart.png";

    private static readonly Color LocationMetadataColor =
        new Color32(4, 27, 45, 255);
    private static readonly Color NormalTimerColor =
        new Color32(58, 11, 70, 255);
    private static readonly Color UrgentTimerColor =
        new Color32(125, 0, 22, 255);

    private static readonly Vector3[] SlotPositions =
    {
        new(0f, 2.25f, 0f),
        new(0f, -1.05f, 0f)
    };

    private static void OnScriptsReloaded()
    {
        EditorApplication.delayCall += TrySetup;
    }

    [MenuItem("Tools/Yeni Ofis/Setup Task Mission Board")]
    public static void SetupFromMenu()
    {
        Setup(true);
    }

    public static void SetupForBatch()
    {
        Setup(false);
    }

    private static void TrySetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Setup(false);
    }

    private static void Setup(bool showDialog)
    {
        Scene originalActiveScene = SceneManager.GetActiveScene();
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
            BuildMissionBoard(officeScene);
            EditorSceneManager.MarkSceneDirty(officeScene);
            EditorSceneManager.SaveScene(officeScene);
        }
        finally
        {
            if (originalActiveScene.IsValid() &&
                originalActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(originalActiveScene);
            }

            if (openedForSetup)
                EditorSceneManager.CloseScene(officeScene, true);
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Görev Kartı",
                "İki yuvalı süreli görev kartı hazır.",
                "Tamam");
        }
    }

    private static void BuildMissionBoard(Scene scene)
    {
        GameObject taskCard = FindObject(scene, "gorev_karti");
        GameObject taskIcon = FindObject(scene, "g\u00F6rev_ikon");
        GameObject trophy = FindObject(scene, "kupa");
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        Sprite cardDecorationSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(CardDecorationPath);

        if (taskCard == null || taskIcon == null || font == null ||
            cardDecorationSprite == null)
        {
            Debug.LogError(
                "Task card setup needs gorev_karti, g\u00F6rev_ikon and " +
                "Kenney Mini Square.ttf plus kulaklık_kart.png.");
            return;
        }

        MoveTaskCardComponents(trophy, taskIcon);

        Transform contentRoot = EnsureChild(
            taskCard.transform,
            "TaskMissionContent");
        contentRoot.localPosition = Vector3.zero;
        contentRoot.localRotation = Quaternion.identity;
        contentRoot.localScale = Vector3.one;

        RemoveCardOverlay(scene, "görev_kartı_boss");
        RemoveCardOverlay(scene, "görev_kartı_siyah");

        GameObject cardDecoration = EnsureCardDecoration(
            scene,
            taskCard.transform,
            new Vector3(-5.4f, -6.55f, 0f),
            cardDecorationSprite);

        TaskCardMissionBoard.TaskSlotView[] slots =
            new TaskCardMissionBoard.TaskSlotView[SlotPositions.Length];

        for (int slotIndex = 0;
             slotIndex < SlotPositions.Length;
             slotIndex++)
        {
            Transform slotRoot = EnsureChild(
                contentRoot,
                $"TaskSlot_{slotIndex + 1}");
            slotRoot.localPosition = SlotPositions[slotIndex];
            slotRoot.localRotation = Quaternion.identity;
            slotRoot.localScale = Vector3.one;

            SpriteRenderer icon = EnsureIcon(slotRoot);
            TextMesh description = EnsureText(
                slotRoot,
                "Description",
                new Vector3(-4.75f, 0.1f, 0f),
                TextAnchor.MiddleLeft,
                TextAlignment.Left,
                72,
                0.095f,
                new Color32(7, 28, 50, 255),
                font);
            TextMesh location = EnsureText(
                slotRoot,
                "Location",
                new Vector3(6.7f, 0.85f, 0f),
                TextAnchor.MiddleRight,
                TextAlignment.Right,
                72,
                0.088f,
                LocationMetadataColor,
                font);
            TextMesh timer = EnsureText(
                slotRoot,
                "Timer",
                new Vector3(6.7f, -0.85f, 0f),
                TextAnchor.MiddleRight,
                TextAlignment.Right,
                76,
                0.095f,
                NormalTimerColor,
                font);

            slots[slotIndex] =
                new TaskCardMissionBoard.TaskSlotView
                {
                    iconRenderer = icon,
                    descriptionText = description,
                    locationText = location,
                    timerText = timer
                };
        }

        TaskCardMissionBoard manager =
            taskIcon.GetComponent<TaskCardMissionBoard>();
        if (manager == null)
            manager = Undo.AddComponent<TaskCardMissionBoard>(taskIcon);

        manager.ConfigureEditor(slots, CreateDefaultTasks());
        ConfigureMetadataColors(manager);
        EditorUtility.SetDirty(manager);

        TaskCardToggle toggle = taskIcon.GetComponent<TaskCardToggle>();
        if (toggle != null)
        {
            SerializedObject toggleObject = new(toggle);
            toggleObject.FindProperty("taskCardDecoration")
                .objectReferenceValue = cardDecoration;
            toggleObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConfigureMetadataColors(
        TaskCardMissionBoard manager)
    {
        SerializedObject managerObject = new(manager);
        managerObject.FindProperty("locationTextColor").colorValue =
            LocationMetadataColor;
        managerObject.FindProperty("normalTimerColor").colorValue =
            NormalTimerColor;
        managerObject.FindProperty("urgentTimerColor").colorValue =
            UrgentTimerColor;
        managerObject.FindProperty("descriptionCharacterSize")
            .floatValue = 0.095f;
        managerObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void MoveTaskCardComponents(
        GameObject source,
        GameObject destination)
    {
        if (source == null || source == destination)
            return;

        CopyComponent<TaskCardToggle>(source, destination);
        CopyComponent<TaskCardMissionBoard>(source, destination);

        SpriteRenderer sourceRenderer =
            source.GetComponent<SpriteRenderer>();
        SpriteRenderer destinationRenderer =
            destination.GetComponent<SpriteRenderer>();
        if (sourceRenderer != null && destinationRenderer != null)
        {
            Undo.RecordObject(
                destinationRenderer,
                "Raise task icon sorting order");
            destinationRenderer.sortingOrder = Mathf.Max(
                destinationRenderer.sortingOrder,
                sourceRenderer.sortingOrder);
            EditorUtility.SetDirty(destinationRenderer);
        }

        Undo.DestroyObjectImmediate(source);
    }

    private static T CopyComponent<T>(
        GameObject source,
        GameObject destination)
        where T : Component
    {
        T sourceComponent = source.GetComponent<T>();
        T destinationComponent = destination.GetComponent<T>();
        if (destinationComponent == null)
            destinationComponent = Undo.AddComponent<T>(destination);

        if (sourceComponent != null)
        {
            Undo.RecordObject(
                destinationComponent,
                $"Move {typeof(T).Name} to task icon");
            EditorUtility.CopySerialized(
                sourceComponent,
                destinationComponent);
            EditorUtility.SetDirty(destinationComponent);
        }

        return destinationComponent;
    }

    private static TaskCardMissionBoard.TaskDefinition[]
        CreateDefaultTasks()
    {
        return new[]
        {
            CreateTask(
                "file_upload",
                "dosya_yükle",
                "Files are late.\nServer is grumpy.",
                "FLOOR 2  |  ROOM 4",
                30f),
            CreateTask(
                "cable_game",
                "kablo_game",
                "Cables made a knot.\nVery professional.",
                "FLOOR 1  |  ROOM 3",
                26f),
            CreateTask(
                "keyboard",
                "pasword_ikon",
                "Password forgot itself.\nPlease remind it.",
                "FLOOR 4  |  ROOM 2",
                45f),
            CreateTask(
                "broken_monitor",
                "monitör",
                "The monitor tried flying.\nIt failed.",
                "FLOOR 3  |  ROOM 5",
                40f),
            CreateTask(
                "mouse",
                "mouse",
                "The mouse escaped.\nNot the furry one.",
                "FLOOR 2  |  ROOM 1",
                28f),
            CreateTask(
                "virus",
                "virus",
                "Viruses moved in.\nThey pay no rent.",
                "FLOOR 3  |  ROOM 2",
                32f),
            CreateTask(
                "wifi",
                "wifi",
                "Wi-Fi vanished.\nClassic Monday.",
                "FLOOR 1  |  ROOM 5",
                30f),
            CreateTask(
                "modem",
                "wifi2",
                "The modem is napping.\nWake it gently.",
                "FLOOR 4  |  ROOM 4",
                36f),
            CreateTaskFromPath(
                "broken_pc",
                "Assets/Art/UI/kasa/kasa_ui.png",
                "The PC gave up.\nPlease negotiate.",
                "FLOOR 1  |  ROOM 4",
                40f),
            CreateTaskFromPath(
                "case_parts",
                "Assets/Art/Görev_assets/bozuk_kasa/parça_kasa_ikon.png",
                "PC parts are loose.\nTiny chaos inside.",
                "FLOOR ?  |  ROOM ?",
                40f),
            CreateTask(
                "email",
                "e_posta",
                "Inbox looks suspicious.\nTrust nobody.",
                "FLOOR 2  |  ROOM 2",
                40f),
            CreateTask(
                "popup_ads",
                "popups_ikon",
                "Pop-ups are multiplying.\nThey found the Wi-Fi.",
                "FLOOR 4  |  ROOM 3",
                75f),
            CreateTaskFromPath(
                "server_cooling",
                "Assets/Art/Görev_assets/Server_Cooling/task_icon.png",
                "Server is sweating.\nOffer technical ice.",
                "FLOOR 4  |  ROOM 1",
                46f),
            CreateTaskFromPath(
                "security_check",
                "Assets/Art/Görev_assets/Security_Check/task_icon.png",
                "Badges look suspicious.\nTrust, but scan.",
                "FLOOR 3  |  ROOM 4",
                50f)
        };
    }

    private static TaskCardMissionBoard.TaskDefinition CreateTask(
        string id,
        string iconName,
        string description,
        string location,
        float duration)
    {
        return CreateTaskFromPath(
            id,
            $"{IconFolder}/{iconName}.png",
            description,
            location,
            duration);
    }

    private static TaskCardMissionBoard.TaskDefinition CreateTaskFromPath(
        string id,
        string iconPath,
        string description,
        string location,
        float duration)
    {
        return new TaskCardMissionBoard.TaskDefinition
        {
            id = id,
            icon = LoadSprite(iconPath),
            description = description,
            location = location,
            duration = duration
        };
    }

    private static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
            return sprite;

        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .FirstOrDefault();
    }

    private static SpriteRenderer EnsureIcon(Transform parent)
    {
        Transform iconTransform = EnsureChild(parent, "Icon");
        iconTransform.localPosition = new Vector3(-6.25f, 0f, 0f);
        iconTransform.localRotation = Quaternion.identity;

        SpriteRenderer renderer =
            iconTransform.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = Undo.AddComponent<SpriteRenderer>(
                iconTransform.gameObject);

        renderer.sortingOrder = 20;
        renderer.color = Color.white;
        EditorUtility.SetDirty(renderer);
        return renderer;
    }

    private static TextMesh EnsureText(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        TextAnchor anchor,
        TextAlignment alignment,
        int fontSize,
        float characterSize,
        Color color,
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
        text.lineSpacing = 0.84f;
        text.anchor = anchor;
        text.alignment = alignment;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.richText = false;

        MeshRenderer renderer =
            textTransform.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.sortingOrder = 21;

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

    private static GameObject EnsureCardDecoration(
        Scene scene,
        Transform cardTransform,
        Vector3 cardLocalPosition,
        Sprite sprite)
    {
        const string decorationName = "kulaklık_kart";
        GameObject decoration = FindObject(scene, decorationName);
        if (decoration == null)
        {
            decoration = new GameObject(decorationName);
            Undo.RegisterCreatedObjectUndo(
                decoration,
                "Create task card decoration");
            SceneManager.MoveGameObjectToScene(decoration, scene);
        }

        Undo.RecordObject(
            decoration.transform,
            "Align task card decoration");

        Vector3 alignedPosition =
            cardTransform.TransformPoint(cardLocalPosition);
        alignedPosition.z = 0f;
        decoration.transform.position = alignedPosition;
        decoration.transform.rotation = Quaternion.identity;
        decoration.transform.localScale = Vector3.one * 0.035f;

        SpriteRenderer renderer =
            decoration.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = Undo.AddComponent<SpriteRenderer>(decoration);

        Undo.RecordObject(renderer, "Configure task card decoration");
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 7;
        EditorUtility.SetDirty(renderer);

        decoration.SetActive(cardTransform.gameObject.activeSelf);
        EditorUtility.SetDirty(decoration.transform);
        return decoration;
    }

    private static void RemoveCardOverlay(
        Scene scene,
        string overlayName)
    {
        GameObject overlay = FindObject(scene, overlayName);
        if (overlay != null)
            Undo.DestroyObjectImmediate(overlay);

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
