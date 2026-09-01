#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeEquipmentMiniGameSceneBuilder
{
    private const string ServerScene = "Assets/Scenes/Server_Cooling.unity";
    private const string SecurityScene = "Assets/Scenes/Security_check.unity";
    private const string FontPath = "Assets/Art/Fonts/Kenney Mini Square.ttf";

    [MenuItem("Tools/Office Game/Build Equipment Mini Games")]
    public static void BuildFromMenu()
    {
        SetupForBatch();
        Debug.Log("Server Cooling and Security Check scenes are ready.");
    }

    public static void SetupForBatch()
    {
        Scene original = SceneManager.GetActiveScene();
        string originalPath = original.path;

        BuildScene(ServerScene, SetupServer);
        BuildScene(SecurityScene, SetupSecurity);
        AddScenesToBuildSettings();

        if (!string.IsNullOrEmpty(originalPath))
            EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildScene(string path, Action<Scene> setup)
    {
        if (!System.IO.File.Exists(path))
            throw new InvalidOperationException($"Scene not found: {path}");

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        ConfigureCamera(scene);
        SetupBackground(scene);
        setup(scene);
        ConfigureBackIcon(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupServer(Scene scene)
    {
        SpriteRenderer hot = Place(scene, "fan_overheated", -0.88f, 1.42f, 0.15f, 24);
        SpriteRenderer stopped = Place(scene, "fan_stopped", 0.88f, 1.42f, 0.15f, 24);
        SpriteRenderer frosty = Place(scene, "fan_frosty", 0f, -0.12f, 0.15f, 24);
        SpriteRenderer running = Place(scene, "fan_running", 1.55f, -1.2f, 0.11f, 18);
        SpriteRenderer canister = Place(scene, "cooling_canister", -1.46f, -3.42f, 0.13f, 30);
        SpriteRenderer wrench = Place(scene, "steel_wrench", 0f, -3.42f, 0.13f, 30);
        SpriteRenderer airflow = Place(scene, "airflow_arrow", 1.46f, -3.42f, 0.13f, 30);
        SpriteRenderer heat = Place(scene, "heat_waves", -0.88f, 2.28f, 0.12f, 28);
        SpriteRenderer snow = Place(scene, "snowflake_burst", 0f, -0.12f, 0.13f, 29);
        SpriteRenderer beacon = Place(scene, "warning_beacon", 1.75f, 2.62f, 0.12f, 32);

        TextMesh progress = EnsureText(scene, "CoolingProgress", new Vector3(0f, 3.72f, 0f),
            "FANS ONLINE  0/3", 0.062f, new Color32(92, 228, 255, 255), 70);
        TextMesh hint = EnsureText(scene, "CoolingHint", new Vector3(0f, 3.23f, 0f),
            "CHOOSE A TOOL", 0.052f, new Color32(255, 219, 93, 255), 70);

        ServerCoolingMiniGame game = EnsureComponent<ServerCoolingMiniGame>(scene, "ServerCoolingGame");
        game.ConfigureEditor(new[] { hot, stopped, frosty }, running, canister,
            wrench, airflow, heat, snow, beacon, progress, hint);
        EditorUtility.SetDirty(game);
    }

    private static void SetupSecurity(Scene scene)
    {
        SpriteRenderer green = Place(scene, "id_green", 0f, 0.45f, 0.17f, 27);
        SpriteRenderer amber = Place(scene, "id_amber", 0f, 0.45f, 0.17f, 26);
        SpriteRenderer red = Place(scene, "id_red", 0f, 0.45f, 0.17f, 26);
        SpriteRenderer damaged = Place(scene, "id_damaged", 0f, 0.45f, 0.17f, 26);
        amber.enabled = red.enabled = damaged.enabled = false;

        SpriteRenderer scanner = Place(scene, "scanner_frame", 0f, 0.45f, 0.22f, 22);
        SpriteRenderer approve = Place(scene, "approve_icon", -1.18f, -3.12f, 0.14f, 31);
        SpriteRenderer reject = Place(scene, "reject_icon", 1.18f, -3.12f, 0.14f, 31);
        SpriteRenderer closedLock = Place(scene, "lock_closed", 0f, 2.48f, 0.13f, 32);
        SpriteRenderer openLock = Place(scene, "lock_open", 0f, 2.48f, 0.13f, 33);
        SpriteRenderer alert = Place(scene, "alert_beacon", 1.72f, 2.58f, 0.12f, 34);
        openLock.enabled = alert.enabled = false;

        TextMesh progress = EnsureText(scene, "SecurityProgress", new Vector3(0f, 3.72f, 0f),
            "BADGES  0/6", 0.062f, new Color32(121, 255, 172, 255), 70);
        TextMesh hint = EnsureText(scene, "SecurityHint", new Vector3(0f, 3.23f, 0f),
            "APPROVE OR REJECT", 0.052f, new Color32(255, 219, 93, 255), 70);

        SecurityCheckMiniGame game = EnsureComponent<SecurityCheckMiniGame>(scene, "SecurityCheckGame");
        game.ConfigureEditor(green,
            new[] { green.sprite, amber.sprite, red.sprite, damaged.sprite },
            approve, reject, scanner, closedLock, openLock, alert, progress, hint);
        EditorUtility.SetDirty(game);
    }

    private static void ConfigureCamera(Scene scene)
    {
        Camera camera = FindObject<Camera>(scene, "Main Camera");
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.backgroundColor = new Color32(8, 17, 28, 255);
        EditorUtility.SetDirty(camera);
    }

    private static void SetupBackground(Scene scene)
    {
        SpriteRenderer background = FindRenderer(scene, "background");
        background.transform.position = Vector3.zero;
        background.transform.rotation = Quaternion.identity;
        background.sortingOrder = 0;
        background.color = Color.white;
        if (background.sprite != null)
        {
            Vector2 size = background.sprite.bounds.size;
            float scale = Mathf.Max(10f / size.y, 5.625f / size.x);
            background.transform.localScale = new Vector3(scale, scale, 1f);
        }
        EditorUtility.SetDirty(background);
    }

    private static void ConfigureBackIcon(Scene scene)
    {
        SpriteRenderer icon = FindRenderer(scene, "geri_ikon");
        icon.gameObject.SetActive(true);
        icon.transform.position = new Vector3(-2.24f, 4.38f, 0f);
        icon.transform.rotation = Quaternion.identity;
        SetVisibleHeight(icon, 0.72f);
        icon.sortingOrder = 100;
        SceneIconButton button = icon.GetComponent<SceneIconButton>();
        if (button == null)
            button = icon.gameObject.AddComponent<SceneIconButton>();
        button.Configure("katlar");
        EditorUtility.SetDirty(icon);
        EditorUtility.SetDirty(button);
    }

    private static SpriteRenderer Place(Scene scene, string name, float x, float y,
        float visibleHeight, int sortingOrder)
    {
        SpriteRenderer renderer = FindRenderer(scene, name);
        renderer.gameObject.SetActive(true);
        renderer.transform.position = new Vector3(x, y, 0f);
        renderer.transform.rotation = Quaternion.identity;
        renderer.color = Color.white;
        renderer.enabled = true;
        renderer.sortingOrder = sortingOrder;
        SetVisibleHeight(renderer, visibleHeight * 10f);
        EditorUtility.SetDirty(renderer);
        return renderer;
    }

    private static void SetVisibleHeight(SpriteRenderer renderer, float targetHeight)
    {
        if (renderer.sprite == null || renderer.sprite.bounds.size.y <= 0.001f)
            return;
        float scale = targetHeight / renderer.sprite.bounds.size.y;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static TextMesh EnsureText(Scene scene, string name, Vector3 position,
        string value, float characterSize, Color color, int sortingOrder)
    {
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
        if (root == null)
        {
            root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        TextMesh text = root.GetComponent<TextMesh>();
        if (text == null)
            text = root.AddComponent<TextMesh>();
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font != null)
        {
            text.font = font;
            text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
        text.text = value;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 64;
        text.characterSize = characterSize;
        text.color = color;
        root.transform.position = position;
        root.transform.localScale = Vector3.one;
        MeshRenderer mesh = text.GetComponent<MeshRenderer>();
        mesh.sortingOrder = sortingOrder;
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(text);
        return text;
    }

    private static T EnsureComponent<T>(Scene scene, string name) where T : Component
    {
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
        if (root == null)
        {
            root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        T component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
    }

    private static SpriteRenderer FindRenderer(Scene scene, string name)
    {
        SpriteRenderer renderer = FindObject<SpriteRenderer>(scene, name);
        if (renderer == null)
            throw new InvalidOperationException($"{scene.name}: missing SpriteRenderer '{name}'.");
        return renderer;
    }

    private static T FindObject<T>(Scene scene, string name) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            T match = components.FirstOrDefault(item => item.gameObject.name == name);
            if (match != null)
                return match;
        }
        return null;
    }

    public static void PruneMissingBuildScenesForBatch()
    {
        EditorBuildSettings.scenes = EditorBuildSettings.scenes
            .Where(scene => System.IO.File.Exists(scene.path))
            .ToArray();
    }

    private static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
            .Where(scene => System.IO.File.Exists(scene.path))
            .ToList();
        foreach (string path in new[] { ServerScene, SecurityScene })
        {
            if (scenes.All(item => !string.Equals(item.path, path, StringComparison.OrdinalIgnoreCase)))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
