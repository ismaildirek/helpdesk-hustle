using UnityEngine;

internal enum HapticFeedbackType
{
    Selection,
    LightImpact,
    Warning,
    Success
}

internal static class DeviceHaptics
{
    private const string HapticsEnabledKey = "Feedback.HapticsEnabled";

    private static bool preferenceLoaded;
    private static bool hapticsEnabled;
    private static double nextAllowedFeedbackTime;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static int androidSdkVersion;
    private static bool androidInitializationAttempted;
    private static bool androidHapticsAvailable;
#endif

    public static bool IsEnabled
    {
        get
        {
            LoadPreference();
            return hapticsEnabled;
        }
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        vibrator?.Dispose();
        vibrator = null;
        androidSdkVersion = 0;
        androidInitializationAttempted = false;
        androidHapticsAvailable = false;
#endif
        preferenceLoaded = false;
        hapticsEnabled = true;
        nextAllowedFeedbackTime = 0d;
    }

    public static void SetEnabled(bool enabled)
    {
        preferenceLoaded = true;
        hapticsEnabled = enabled;
        PlayerPrefs.SetInt(HapticsEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void PlayForSound(GameSound sound)
    {
        switch (sound)
        {
            case GameSound.UiClick:
            case GameSound.CablePickup:
            case GameSound.KeyPress:
                Play(HapticFeedbackType.Selection);
                break;

            case GameSound.WrongAction:
            case GameSound.BossWarning:
                Play(HapticFeedbackType.Warning);
                break;

            case GameSound.TaskCompleted:
                Play(HapticFeedbackType.Success);
                break;

            default:
                Play(HapticFeedbackType.LightImpact);
                break;
        }
    }

    public static void Play(HapticFeedbackType feedbackType)
    {
        LoadPreference();
        if (!hapticsEnabled)
            return;

        double now = Time.realtimeSinceStartupAsDouble;
        float cooldown = GetCooldown(feedbackType);
        if (now < nextAllowedFeedbackTime)
            return;

        nextAllowedFeedbackTime = now + cooldown;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!EnsureAndroidHaptics())
            return;

        try
        {
            long durationMilliseconds = GetDurationMilliseconds(
                feedbackType);
            int amplitude = GetAmplitude(feedbackType);

            if (androidSdkVersion >= 26)
            {
                using AndroidJavaClass vibrationEffectClass = new(
                    "android.os.VibrationEffect");
                using AndroidJavaObject effect =
                    vibrationEffectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot",
                        durationMilliseconds,
                        amplitude);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", durationMilliseconds);
            }
        }
        catch (AndroidJavaException exception)
        {
            androidHapticsAvailable = false;
            Debug.LogWarning(
                $"Android haptics disabled after a device error: {exception.Message}");
        }
#endif
    }

    private static void LoadPreference()
    {
        if (preferenceLoaded)
            return;

        hapticsEnabled = PlayerPrefs.GetInt(HapticsEnabledKey, 1) != 0;
        preferenceLoaded = true;
    }

    private static float GetCooldown(HapticFeedbackType feedbackType)
    {
        return feedbackType switch
        {
            HapticFeedbackType.Selection => 0.045f,
            HapticFeedbackType.LightImpact => 0.07f,
            HapticFeedbackType.Warning => 0.14f,
            HapticFeedbackType.Success => 0.18f,
            _ => 0.07f
        };
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool EnsureAndroidHaptics()
    {
        if (androidInitializationAttempted)
            return androidHapticsAvailable;

        androidInitializationAttempted = true;
        try
        {
            using AndroidJavaClass unityPlayer = new(
                "com.unity3d.player.UnityPlayer");
            using AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>(
                    "currentActivity");
            vibrator = activity.Call<AndroidJavaObject>(
                "getSystemService",
                "vibrator");

            using AndroidJavaClass versionClass = new(
                "android.os.Build$VERSION");
            androidSdkVersion = versionClass.GetStatic<int>("SDK_INT");
            androidHapticsAvailable =
                vibrator != null && vibrator.Call<bool>("hasVibrator");
        }
        catch (AndroidJavaException exception)
        {
            androidHapticsAvailable = false;
            Debug.LogWarning(
                $"Android haptics are unavailable: {exception.Message}");
        }

        return androidHapticsAvailable;
    }

    private static long GetDurationMilliseconds(
        HapticFeedbackType feedbackType)
    {
        return feedbackType switch
        {
            HapticFeedbackType.Selection => 8L,
            HapticFeedbackType.LightImpact => 12L,
            HapticFeedbackType.Warning => 24L,
            HapticFeedbackType.Success => 32L,
            _ => 12L
        };
    }

    private static int GetAmplitude(HapticFeedbackType feedbackType)
    {
        return feedbackType switch
        {
            HapticFeedbackType.Selection => 45,
            HapticFeedbackType.LightImpact => 65,
            HapticFeedbackType.Warning => 110,
            HapticFeedbackType.Success => 125,
            _ => 65
        };
    }
#endif
}