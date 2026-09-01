using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

public static class AndroidReleaseReadiness
{
    private const string EntranceScenePath =
        "Assets/Scenes/Giris_Ekran.unity";
    private const string DefaultIdentifier =
        "com.DefaultCompany.Staj_Projesi1";
    private const string OutputDirectory = "Builds/Android";
    private const string ValidationBundleName =
        "HelpdeskHustle-validation.aab";
    private const string LauncherIconPath =
        "Assets/Art/UI/Store/helpdesk_hustle_launcher_icon.png";

    [MenuItem("Tools/Release/Apply Safe Android Settings")]
    public static void ApplySafeAndroidSettings()
    {
        PlayerSettings.productName = "Helpdesk Hustle";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(
            1,
            PlayerSettings.Android.bundleVersionCode);

        PlayerSettings.defaultInterfaceOrientation =
            UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.Android.renderOutsideSafeArea = false;
        PlayerSettings.runInBackground = false;

        PlayerSettings.Android.minSdkVersion =
            AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion =
            (AndroidSdkVersions)36;
        PlayerSettings.Android.targetArchitectures =
            AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(
            NamedBuildTarget.Android,
            ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(
            NamedBuildTarget.Android,
            Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetManagedStrippingLevel(
            NamedBuildTarget.Android,
            ManagedStrippingLevel.Medium);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.gcIncremental = true;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetGraphicsAPIs(
            BuildTarget.Android,
            new[] { GraphicsDeviceType.OpenGLES3 });

        EditorUserBuildSettings.androidBuildSystem =
            AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.androidBuildSubtarget =
            MobileTextureSubtarget.ETC2;
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.androidCreateSymbols =
            AndroidCreateSymbols.Public;
        EditorUserBuildSettings.androidCreateSymbolsZip = true;

        ApplyLauncherIconIfAvailable();

        AssetDatabase.SaveAssets();
        Debug.Log(
            "Safe Android release settings applied. " +
            "Final package ID and release keystore still require " +
            "owner-provided values.");
    }

    [MenuItem("Tools/Release/Validate Android Readiness")]
    public static void ValidateAndroidReadiness()
    {
        AndroidReadinessResult result = CollectReadiness();
        string report = result.Format();

        if (result.Errors.Length > 0)
        {
            Debug.LogError(report);
            throw new BuildFailedException(
                "Android release readiness validation failed.");
        }

        if (result.Warnings.Length > 0)
            Debug.LogWarning(report);
        else
            Debug.Log(report);
    }

    public static void ApplyAndValidate()
    {
        ApplySafeAndroidSettings();
        ValidateAndroidReadiness();
    }

    [MenuItem("Tools/Release/Build Validation AAB")]
    public static void BuildValidationAab()
    {
        BuildValidationAabInternal(true);
    }

    [MenuItem("Tools/Release/Build Incremental Validation AAB")]
    public static void BuildIncrementalValidationAab()
    {
        BuildValidationAabInternal(false);
    }

    private static void BuildValidationAabInternal(
        bool cleanBuildCache)
    {
        ApplySafeAndroidSettings();
        ValidateAndroidReadiness();

        Directory.CreateDirectory(OutputDirectory);
        string outputPath = Path.Combine(
            OutputDirectory,
            ValidationBundleName);
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        BuildPlayerOptions options = new()
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = cleanBuildCache
                ? BuildOptions.CleanBuildCache
                : BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        string summaryText =
            $"Result: {summary.result}{Environment.NewLine}" +
            $"Output: {Path.GetFullPath(outputPath)}{Environment.NewLine}" +
            $"Size: {summary.totalSize} bytes{Environment.NewLine}" +
            $"Warnings: {summary.totalWarnings}{Environment.NewLine}" +
            $"Errors: {summary.totalErrors}{Environment.NewLine}" +
            $"Duration: {summary.totalTime}";
        File.WriteAllText(
            Path.Combine(OutputDirectory, "build-summary.txt"),
            summaryText);

        if (summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(summaryText);

        Debug.Log(
            summaryText + Environment.NewLine +
            "This validation AAB uses the local debug key until a " +
            "release upload keystore is configured.");
    }

    public static AndroidReadinessResult CollectReadiness()
    {
        System.Collections.Generic.List<string> errors = new();
        System.Collections.Generic.List<string> warnings = new();

        EditorBuildSettingsScene[] scenes =
            EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
        if (scenes.Length == 0)
            errors.Add("No enabled build scenes were found.");
        else if (!string.Equals(
                     scenes[0].path,
                     EntranceScenePath,
                     StringComparison.Ordinal))
            errors.Add("Giris_Ekran must be the first enabled build scene.");

        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (!File.Exists(scene.path))
                errors.Add($"Build scene is missing: {scene.path}");
        }

        if (PlayerSettings.GetScriptingBackend(
                NamedBuildTarget.Android) !=
            ScriptingImplementation.IL2CPP)
            errors.Add("Android scripting backend must be IL2CPP.");

        if ((PlayerSettings.Android.targetArchitectures &
             AndroidArchitecture.ARM64) == 0)
            errors.Add("ARM64 architecture is required.");

        if ((int)PlayerSettings.Android.targetSdkVersion < 36)
            errors.Add("Target API must be Android 16 / API 36 or newer.");

        if (!EditorUserBuildSettings.buildAppBundle)
            errors.Add("Build App Bundle must be enabled.");

        if (PlayerSettings.Android.renderOutsideSafeArea)
            errors.Add("Rendering outside the Android safe area is enabled.");

        string identifier = PlayerSettings.GetApplicationIdentifier(
            NamedBuildTarget.Android);
        if (string.IsNullOrWhiteSpace(identifier) ||
            string.Equals(
                identifier,
                DefaultIdentifier,
                StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                "Final Android package identifier has not been chosen.");
        }

        if (!PlayerSettings.Android.useCustomKeystore)
            warnings.Add("Release upload keystore is not configured.");

        if (!HasAndroidLauncherIcon())
            warnings.Add("Android launcher icon is not configured.");

        if (!HasThirdPartyLicenseEvidence())
        {
            warnings.Add(
                "No local license/receipt evidence was found for the " +
                "Casual & Relaxing Game Music package (Asset Store " +
                "product 262740). Preserve its purchase/license proof.");
        }

        return new AndroidReadinessResult(
            errors.ToArray(),
            warnings.ToArray());
    }

    private static bool HasAndroidLauncherIcon()
    {
#pragma warning disable CS0618
        Texture2D[] icons = PlayerSettings.GetIconsForTargetGroup(
            BuildTargetGroup.Android);
#pragma warning restore CS0618
        return icons != null && icons.Any(icon => icon != null);
    }

    private static void ApplyLauncherIconIfAvailable()
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
            LauncherIconPath);
        if (icon == null)
            return;

#pragma warning disable CS0618
        PlayerSettings.SetIconsForTargetGroup(
            BuildTargetGroup.Android,
            new[] { icon });
#pragma warning restore CS0618
    }

    private static bool HasThirdPartyLicenseEvidence()
    {
        string musicDirectory =
            "Assets/Casual & Relaxing Game Music";
        if (!Directory.Exists(musicDirectory))
            return true;

        return Directory.EnumerateFiles(
                musicDirectory,
                "*",
                SearchOption.AllDirectories)
            .Any(path =>
                path.IndexOf("license", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("receipt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("invoice", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public readonly struct AndroidReadinessResult
    {
        public AndroidReadinessResult(string[] errors, string[] warnings)
        {
            Errors = errors ?? Array.Empty<string>();
            Warnings = warnings ?? Array.Empty<string>();
        }

        public string[] Errors { get; }
        public string[] Warnings { get; }

        public string Format()
        {
            string errorText = Errors.Length == 0
                ? "None"
                : string.Join(Environment.NewLine + "- ", Errors);
            string warningText = Warnings.Length == 0
                ? "None"
                : string.Join(Environment.NewLine + "- ", Warnings);
            return
                "ANDROID RELEASE READINESS" + Environment.NewLine +
                "Errors:" + Environment.NewLine + "- " + errorText +
                Environment.NewLine +
                "Warnings:" + Environment.NewLine + "- " + warningText;
        }
    }
}


public sealed class AndroidReleaseManifestPostprocessor :
    IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(
            path,
            "src",
            "main",
            "AndroidManifest.xml");
        if (!File.Exists(manifestPath))
            return;

        string[] lines = File.ReadAllLines(manifestPath);
        string[] filtered = lines
            .Where(line => !line.Contains(
                "android.permission.INTERNET",
                StringComparison.Ordinal))
            .ToArray();
        if (filtered.Length == lines.Length)
            return;

        File.WriteAllLines(manifestPath, filtered);
        Debug.Log(
            "Removed unused INTERNET permission from Android manifest.");
    }
}
