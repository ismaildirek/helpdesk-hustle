using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns mobile-only application lifecycle and Android system-back behavior.
/// It is installed before the first scene and survives scene transitions.
/// </summary>
[DisallowMultipleComponent]
public sealed class MobileReleaseRuntime : MonoBehaviour
{
    private const string EntranceSceneName = "Giris_Ekran";
    private const string OfficeSceneName = "YeniOfis";
    private const string FloorsSceneName = "katlar";

    private static MobileReleaseRuntime instance;

    private bool applicationPaused;
    private bool applicationFocused = true;
    private bool pausedByLifecycle;
    private bool navigationRequested;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null)
            return;

        GameObject runtimeObject = new("Mobile Release Runtime");
        instance = runtimeObject.AddComponent<MobileReleaseRuntime>();
        DontDestroyOnLoad(runtimeObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
#if UNITY_ANDROID
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleAndroidBackButton();
        }
#endif
    }

    private void OnApplicationPause(bool paused)
    {
        if (!Application.isMobilePlatform)
            return;

        applicationPaused = paused;
        RefreshLifecyclePause();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!Application.isMobilePlatform)
            return;

        applicationFocused = focused;
        RefreshLifecyclePause();
    }

    private void RefreshLifecyclePause()
    {
        bool shouldSuspend = applicationPaused || !applicationFocused;
        if (shouldSuspend)
        {
            if (!GamePauseSession.IsPaused)
            {
                GamePauseSession.SetPaused(true);
                pausedByLifecycle = true;
            }

            return;
        }

        if (!pausedByLifecycle)
            return;

        pausedByLifecycle = false;
        GamePauseSession.SetPaused(false);
    }

    private void HandleAndroidBackButton()
    {
        if (navigationRequested || BossIntroDialogue.IsBlockingOfficeInput)
            return;

        string activeScene = SceneManager.GetActiveScene().name;

        if (activeScene == OfficeSceneName)
        {
            if (OfficeHelpOverlay.CloseFromSystemBack())
                return;

            OfficeNavigationMenu.HandleSystemBack();
            return;
        }

        if (activeScene == EntranceSceneName)
        {
            Application.Quit();
            return;
        }

        string destination = GetBackDestination(activeScene);
        if (string.IsNullOrEmpty(destination))
            return;

        navigationRequested = true;
        TaskMissionSession.AbandonLaunchedTaskForScene(activeScene);
        if (!BossAngerSession.HasLost)
            SceneManager.LoadScene(destination);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        navigationRequested = false;
        Application.targetFrameRate = 60;
    }

    public static string GetBackDestination(string activeScene)
    {
        if (string.Equals(
                activeScene,
                FloorsSceneName,
                System.StringComparison.Ordinal))
        {
            return OfficeSceneName;
        }

        if (string.Equals(
                activeScene,
                EntranceSceneName,
                System.StringComparison.Ordinal) ||
            string.Equals(
                activeScene,
                OfficeSceneName,
                System.StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return FloorsSceneName;
    }
}
