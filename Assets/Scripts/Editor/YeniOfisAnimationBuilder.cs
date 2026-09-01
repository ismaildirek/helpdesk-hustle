using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// YeniOfis sahnesindeki patron karakterleri için verilen kare dizilerinden
/// sprite animasyonlarını üretir. Unity yeniden derlendiğinde bir kez çalışır;
/// daha sonra menüden yeniden üretilebilir.
/// </summary>
public static class YeniOfisAnimationBuilder
{
    private const string ScenePath = "Assets/Scenes/YeniOfis.unity";
    private const string OutputFolder = "Assets/Animations/OfisYeni";
    private const float BossFramesPerSecond = 8f;
    private const float WorkerFramesPerSecond = 3f;
    [MenuItem("Tools/Yeni Ofis/Animasyonlari Olustur")]
    public static void Build()
    {
        EnsureAssetFolder(OutputFolder);

        var firstSequence = new[] { 1, 1, 2, 1, 1, 1, 6, 1 };
        var secondSequence = new[] { 1, 6, 1, 4, 1, 5 };

        var firstClip = CreateOrUpdateClip("Patron_Dizi_1", firstSequence.Select(GetMainSprite), BossFramesPerSecond);
        var secondClip = CreateOrUpdateClip("Patron_Dizi_2", secondSequence.Select(GetMainSprite), BossFramesPerSecond);
        var firstController = CreateOrUpdateController("Patron_Dizi_1", firstClip);
        var secondController = CreateOrUpdateController("Patron_Dizi_2", secondClip);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CreateOrUpdateAnimatedCharacter(
            "Patron Animasyon 1 (1-1-2-1-1-1-6-1)",
            firstSequence[0],
            firstController,
            new Vector3(-1.8f, 0f, 0f));
        CreateOrUpdateAnimatedCharacter(
            "Patron Animasyon 2 (1-6-1-4-1-5)",
            secondSequence[0],
            secondController,
            new Vector3(1.8f, 0f, 0f));

        CreateOrUpdateWorkerAnimations();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("YeniOfis animasyonlari olusturuldu.");
    }

    private static AnimationClip CreateOrUpdateClip(string name, System.Collections.Generic.IEnumerable<Sprite> sequence, float framesPerSecond)
    {
        var path = $"{OutputFolder}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = framesPerSecond;
        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        var frames = sequence
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / framesPerSecond,
                value = sprite
            })
            .ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        var serializedClip = new SerializedObject(clip);
        serializedClip.FindProperty("m_AnimationClipSettings").FindPropertyRelative("m_LoopTime").boolValue = true;
        serializedClip.ApplyModifiedProperties();
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void CreateOrUpdateWorkerAnimations()
    {
        var sourceRoot = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Art", "OfisYeni");
        var workerFolders = Directory.GetDirectories(sourceRoot, "cal*")
            .Where(folder => Directory.GetFiles(folder, "*.png").Length > 0)
            .OrderBy(ExtractTrailingNumber)
            .ToArray();

        for (var workerIndex = 0; workerIndex < workerFolders.Length; workerIndex++)
        {
            var folder = workerFolders[workerIndex];
            var orderedAssetPaths = Directory.GetFiles(folder, "*.png")
                .OrderBy(path => ExtractTrailingNumber(Path.GetFileNameWithoutExtension(path)))
                .ThenBy(Path.GetFileName)
                .Select(ToAssetPath)
                .ToArray();
            var sourceSprites = orderedAssetPaths.Select(GetMainSprite).ToArray();
            if (sourceSprites.Length == 0)
                continue;

            var slowSequence = sourceSprites.SelectMany(sprite => new[] { sprite, sprite });
            var workerNumber = ExtractTrailingNumber(folder);
            var animationName = $"Calisan_{workerNumber}_Uzun_Yavas";
            var clip = CreateOrUpdateClip(animationName, slowSequence, WorkerFramesPerSecond);
            var controller = CreateOrUpdateController(animationName, clip);
            var horizontalPosition = (workerIndex - (workerFolders.Length - 1) / 2f) * 1.8f;

            CreateOrUpdateAnimatedCharacter(
                $"Calisan {workerNumber} Uzun Yavas Animasyon",
                sourceSprites[0],
                controller,
                new Vector3(horizontalPosition, -2.4f, 0f),
                0.25f,
                11);
        }
    }

    private static AnimatorController CreateOrUpdateController(string name, AnimationClip clip)
    {
        var path = $"{OutputFolder}/{name}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        var stateMachine = controller.layers[0].stateMachine;
        var state = stateMachine.states
            .Select(item => item.state)
            .FirstOrDefault(item => item.name == "Oynat");
        if (state == null)
            state = stateMachine.AddState("Oynat");

        state.motion = clip;
        stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void CreateOrUpdateAnimatedCharacter(string objectName, int initialSprite, RuntimeAnimatorController controller, Vector3 position)
    {
        CreateOrUpdateAnimatedCharacter(objectName, GetMainSprite(initialSprite), controller, position, 0.45f, 10);
    }

    private static void CreateOrUpdateAnimatedCharacter(string objectName, Sprite initialSprite, RuntimeAnimatorController controller, Vector3 position, float scale, int sortingOrder)
    {
        var character = GameObject.Find(objectName);
        if (character == null)
            character = new GameObject(objectName);

        character.transform.position = position;
        character.transform.localScale = Vector3.one * scale;

        var renderer = character.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = character.AddComponent<SpriteRenderer>();
        renderer.sprite = initialSprite;
        renderer.sortingOrder = sortingOrder;

        var animator = character.GetComponent<Animator>();
        if (animator == null)
            animator = character.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
    }

    private static Sprite GetMainSprite(int number)
    {
        return GetMainSprite(
            $"Assets/Art/OfisYeni/patron/boss_sprite_{number}.png");
    }

    private static Sprite GetMainSprite(string path)
    {
        var sprite = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderByDescending(candidate => candidate.rect.width * candidate.rect.height)
            .FirstOrDefault();

        if (sprite == null)
            throw new System.InvalidOperationException($"Sprite bulunamadi: {path}");
        return sprite;
    }

    private static int ExtractTrailingNumber(string value)
    {
        var match = Regex.Match(value, @"(\d+)$");
        return match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
    }

    private static string ToAssetPath(string absolutePath)
    {
        var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), absolutePath);
        return relativePath.Replace('\\', '/');
    }

    private static void EnsureAssetFolder(string assetPath)
    {
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        if (!Directory.Exists(absolutePath))
            Directory.CreateDirectory(absolutePath);
    }
}
