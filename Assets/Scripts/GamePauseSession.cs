using UnityEngine;

internal static class GamePauseSession
{
    private static float previousTimeScale = 1f;

    public static bool IsPaused { get; private set; }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        IsPaused = false;
        previousTimeScale = 1f;
    }

    public static void BeginNewGame()
    {
        if (IsPaused)
            SetPaused(false);

        IsPaused = false;
        previousTimeScale = 1f;
    }

    public static void Toggle()
    {
        SetPaused(!IsPaused);
    }

    public static void SetPaused(bool paused)
    {
        if (IsPaused == paused)
            return;

        if (paused)
        {
            previousTimeScale = Time.timeScale > 0f
                ? Time.timeScale
                : 1f;
            TaskMissionSession.PauseTimers();
            IsPaused = true;
            Time.timeScale = 0f;
            return;
        }

        TaskMissionSession.ResumeTimers();
        IsPaused = false;
        Time.timeScale = previousTimeScale > 0f
            ? previousTimeScale
            : 1f;
    }
}
