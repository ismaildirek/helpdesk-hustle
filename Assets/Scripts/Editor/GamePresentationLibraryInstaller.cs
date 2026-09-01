using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GamePresentationLibraryInstaller
{
    private const string LibraryPath =
        "Assets/Resources/GamePresentationLibrary.asset";
    private const string ForestPath =
        "Assets/Casual & Relaxing Game Music/Forest.wav";
    private const string HappyPath =
        "Assets/Casual & Relaxing Game Music/Happy.wav";
    private const string MiniGameMusicFolder =
        "Assets/Audio/MiniGameMusic";
    private const string FontPath =
        "Assets/Art/Fonts/Kenney Mini Square.ttf";
    private const string ResumeIconPath =
        "Assets/Art/UI/başlat.png";
    private const float MiniGameMusicGain = 0.16f;

    private sealed class MusicMapping
    {
        public readonly string SceneName;
        public readonly string[] FileNames;

        public MusicMapping(string sceneName, params string[] fileNames)
        {
            SceneName = sceneName;
            FileNames = fileNames;
        }
    }

    private static readonly MusicMapping[] MiniGameMappings =
    {
        new("kablo_game", "01_cable_current.wav"),
        new(
            "Dosya_Yükle",
            "02_upload_uplink.wav",
            "12_file_flow.wav"),
        new("virüs", "03_virus_rush.wav", "13_office_alert.wav"),
        new("bozukkasa", "04_repair_rhythm.wav"),
        new(
            "bozukmonitör",
            "05_monitor_reboot.wav",
            "14_system_restore.wav"),
        new("e_posta", "06_inbox_panic.wav"),
        new("popup_ads", "07_popup_patrol.wav"),
        new("pasword_game", "08_password_pulse.wav"),
        new("wifi_sinyal", "09_wifi_hunt.wav"),
        new("modem", "10_modem_wakeup.wav"),
        new(
            "kasa_parça",
            "11_parts_shuffle.wav",
            "15_task_complete.wav"),
        new("Server_Cooling", "17_server_cooling.wav"),
        new("Security_check", "18_security_scan.wav")
    };

    static GamePresentationLibraryInstaller()
    {
        EditorApplication.delayCall += EnsureLibrary;
    }

    [MenuItem("Tools/Office Game/Rebuild Presentation Library")]
    private static void RebuildFromMenu()
    {
        EnsureLibrary();
    }

    public static void RebuildForBatch()
    {
        EnsureLibrary();
    }

    private static void EnsureLibrary()
    {
        ConfigureMiniGameImportSettings();

        AudioClip forest = AssetDatabase.LoadAssetAtPath<AudioClip>(
            ForestPath);
        AudioClip happy = AssetDatabase.LoadAssetAtPath<AudioClip>(
            HappyPath);
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        Sprite resumeIcon = AssetDatabase.LoadAssetAtPath<Sprite>(
            ResumeIconPath);
        GamePresentationLibrary.SceneMusicEntry[] miniGameMusic =
            CreateMiniGameMusicEntries();

        if (forest == null ||
            happy == null ||
            font == null ||
            resumeIcon == null ||
            miniGameMusic == null)
        {
            Debug.LogWarning(
                "Presentation library could not find all required music, font or icon assets.");
            return;
        }

        GamePresentationLibrary library =
            AssetDatabase.LoadAssetAtPath<GamePresentationLibrary>(
                LibraryPath);

        if (library == null)
        {
            Directory.CreateDirectory("Assets/Resources");
            library = ScriptableObject.CreateInstance<
                GamePresentationLibrary>();
            library.ConfigureEditor(
                forest,
                happy,
                font,
                resumeIcon,
                miniGameMusic);
            AssetDatabase.CreateAsset(library, LibraryPath);
            AssetDatabase.SaveAssets();
            return;
        }

        if (!library.ConfigureEditor(
                forest,
                happy,
                font,
                resumeIcon,
                miniGameMusic))
        {
            return;
        }

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
    }

    private static GamePresentationLibrary.SceneMusicEntry[]
        CreateMiniGameMusicEntries()
    {
        List<GamePresentationLibrary.SceneMusicEntry> entries = new();

        foreach (MusicMapping mapping in MiniGameMappings)
        {
            AudioClip[] clips = new AudioClip[mapping.FileNames.Length];
            for (int index = 0; index < mapping.FileNames.Length; index++)
            {
                string assetPath =
                    $"{MiniGameMusicFolder}/{mapping.FileNames[index]}";
                clips[index] = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    assetPath);
                if (clips[index] == null)
                {
                    Debug.LogWarning(
                        $"Mini-game music is missing: {assetPath}");
                    return null;
                }
            }

            entries.Add(new GamePresentationLibrary.SceneMusicEntry(
                mapping.SceneName,
                clips,
                MiniGameMusicGain));
        }

        return entries.ToArray();
    }

    private static void ConfigureMiniGameImportSettings()
    {
        foreach (MusicMapping mapping in MiniGameMappings)
        {
            foreach (string fileName in mapping.FileNames)
            {
                string assetPath = $"{MiniGameMusicFolder}/{fileName}";
                AudioImporter importer =
                    AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (importer == null)
                    continue;

                AudioImporterSampleSettings settings =
                    importer.defaultSampleSettings;
                bool requiresImport =
                    !importer.forceToMono ||
                    !settings.preloadAudioData ||
                    importer.loadInBackground ||
                    settings.loadType !=
                        AudioClipLoadType.CompressedInMemory ||
                    settings.compressionFormat !=
                        AudioCompressionFormat.Vorbis ||
                    !Mathf.Approximately(settings.quality, 0.55f) ||
                    settings.sampleRateSetting !=
                        AudioSampleRateSetting.OptimizeSampleRate;

                if (!requiresImport)
                    continue;

                importer.forceToMono = true;
                settings.preloadAudioData = true;
                importer.loadInBackground = false;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.55f;
                settings.sampleRateSetting =
                    AudioSampleRateSetting.OptimizeSampleRate;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }
    }
}