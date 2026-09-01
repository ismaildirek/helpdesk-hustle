using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

internal readonly struct TaskMissionRoute
{
    public TaskMissionRoute(
        string sceneName,
        int floor,
        int room)
    {
        SceneName = sceneName;
        Floor = floor;
        Room = room;
    }

    public string SceneName { get; }
    public int Floor { get; }
    public int Room { get; }
}

internal readonly struct TaskMissionSnapshot
{
    public TaskMissionSnapshot(
        string taskId,
        string sceneName,
        int floor,
        int room,
        float remainingTime)
    {
        TaskId = taskId;
        SceneName = sceneName;
        Floor = floor;
        Room = room;
        RemainingTime = remainingTime;
    }

    public string TaskId { get; }
    public string SceneName { get; }
    public int Floor { get; }
    public int Room { get; }
    public float RemainingTime { get; }
}

internal static class TaskMissionRouteCatalog
{
    public static bool TryGetRoute(
        string taskId,
        out TaskMissionRoute route)
    {
        switch (taskId)
        {
            case "file_upload":
                route = new TaskMissionRoute(
                    "Dosya_Y\u00FCkle",
                    2,
                    4);
                return true;

            case "cable_game":
                route = new TaskMissionRoute(
                    "kablo_game",
                    1,
                    3);
                return true;

            case "broken_monitor":
                route = new TaskMissionRoute(
                    "bozukmonit\u00F6r",
                    3,
                    5);
                return true;

            case "virus":
                route = new TaskMissionRoute(
                    "vir\u00FCs",
                    3,
                    2);
                return true;

            case "modem":
                route = new TaskMissionRoute(
                    "modem",
                    4,
                    4);
                return true;

            case "broken_pc":
                route = new TaskMissionRoute(
                    "bozukkasa",
                    1,
                    4);
                return true;

            case "email":
                route = new TaskMissionRoute(
                    "e_posta",
                    2,
                    2);
                return true;

            case "popup_ads":
                route = new TaskMissionRoute(
                    "popup_ads",
                    4,
                    3);
                return true;

            case "keyboard":
                route = new TaskMissionRoute(
                    "pasword_game",
                    4,
                    2);
                return true;

            case "wifi":
                route = new TaskMissionRoute(
                    "wifi_sinyal",
                    1,
                    5);
                return true;

            case "case_parts":
                route = new TaskMissionRoute(
                    "kasa_parça",
                    1,
                    1);
                return true;


            case "server_cooling":
                route = new TaskMissionRoute(
                    "Server_Cooling",
                    4,
                    1);
                return true;

            case "security_check":
                route = new TaskMissionRoute(
                    "Security_check",
                    3,
                    4);
                return true;

            default:
                route = default;
                return false;
        }
    }

    public static bool TryCreateAssignmentRoute(
        string taskId,
        out TaskMissionRoute route)
    {
        if (!TryGetRoute(taskId, out route))
            return false;

        if (!string.Equals(
                taskId,
                "case_parts",
                StringComparison.Ordinal))
        {
            return true;
        }

        const int attemptCount = 24;
        for (int attempt = 0; attempt < attemptCount; attempt++)
        {
            int floor = UnityEngine.Random.Range(1, 5);
            int room = UnityEngine.Random.Range(1, 6);
            if (TaskMissionSession.IsRoomOccupied(floor, room))
                continue;

            route = new TaskMissionRoute("kasa_parça", floor, room);
            return true;
        }

        return true;
    }
}

internal static class TaskMissionSession
{
    private sealed class ActiveTask
    {
        public string taskId;
        public TaskMissionRoute route;
        public double expiresAt;
        public float duration;
        public bool timerPaused;
        public double timerPausedAt;
    }

    private static ActiveTask[] activeTasks =
        Array.Empty<ActiveTask>();
    private static string[] completedTaskIds =
        Array.Empty<string>();
    private static int launchedSlotIndex = -1;
    private static string launchedTaskId;
    private static string launchedSceneName;
    private static bool timersPaused;
    private static double timersPausedAt;

#if UNITY_EDITOR
    private static bool timerNowOverrideEnabled;
    private static double timerNowOverride;
#endif

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        activeTasks = Array.Empty<ActiveTask>();
        completedTaskIds = Array.Empty<string>();
        timersPaused = false;
        timersPausedAt = 0d;
#if UNITY_EDITOR
        timerNowOverrideEnabled = false;
        timerNowOverride = 0d;
#endif
        ClearLaunchedTask();
    }

    public static void BeginNewGame()
    {
        ResetSession();
    }

    public static void EnsureSlotCount(int slotCount)
    {
        int safeCount = Mathf.Max(0, slotCount);
        if (activeTasks.Length == safeCount &&
            completedTaskIds.Length == safeCount)
            return;

        ActiveTask[] resizedTasks = new ActiveTask[safeCount];
        string[] resizedCompletedTaskIds = new string[safeCount];
        Array.Copy(
            activeTasks,
            resizedTasks,
            Mathf.Min(activeTasks.Length, resizedTasks.Length));
        Array.Copy(
            completedTaskIds,
            resizedCompletedTaskIds,
            Mathf.Min(
                completedTaskIds.Length,
                resizedCompletedTaskIds.Length));
        activeTasks = resizedTasks;
        completedTaskIds = resizedCompletedTaskIds;
    }

    public static void AssignTask(
        int slotIndex,
        string taskId,
        TaskMissionRoute route,
        float duration)
    {
        if (!IsValidSlot(slotIndex))
            return;

        float safeDuration = Mathf.Max(0f, duration);
        activeTasks[slotIndex] = new ActiveTask
        {
            taskId = taskId,
            route = route,
            expiresAt = GetTimerNow() + safeDuration,
            duration = safeDuration
        };
        completedTaskIds[slotIndex] = null;
    }

    public static void ClearTask(int slotIndex)
    {
        if (IsValidSlot(slotIndex))
        {
            activeTasks[slotIndex] = null;
            completedTaskIds[slotIndex] = null;

            if (launchedSlotIndex == slotIndex)
                ClearLaunchedTask();
        }
    }

    public static bool ExpireTask(int slotIndex)
    {
        return FailActiveTask(
            slotIndex,
            BossAngerFailureReason.TaskExpired);
    }

    public static bool CompleteLaunchedTaskForScene(string sceneName)
    {
        if (!IsValidSlot(launchedSlotIndex) ||
            string.IsNullOrWhiteSpace(sceneName) ||
            !string.Equals(
                launchedSceneName,
                sceneName,
                StringComparison.Ordinal))
        {
            return false;
        }

        ActiveTask launchedTask = activeTasks[launchedSlotIndex];
        if (launchedTask == null ||
            !string.Equals(
                launchedTask.taskId,
                launchedTaskId,
                StringComparison.Ordinal) ||
            !string.Equals(
                launchedTask.route.SceneName,
                sceneName,
                StringComparison.Ordinal))
        {
            ClearLaunchedTask();
            return false;
        }

        float remainingTime = GetRemainingTime(launchedTask);
        if (remainingTime <= 0f)
        {
            FailActiveTask(
                launchedSlotIndex,
                BossAngerFailureReason.TaskExpired);
            return false;
        }

        remainingTime = Mathf.Max(0f, remainingTime);
        TaskQualityResult quality = MiniGamePerformanceSession.Complete(
            sceneName,
            remainingTime,
            launchedTask.duration);
        completedTaskIds[launchedSlotIndex] = launchedTask.taskId;
        activeTasks[launchedSlotIndex] = null;
        int awardedScore = GameProgressionSession.RegisterTaskCompleted(
            remainingTime,
            quality);
        bool bossCalmed =
            GameProgressionSession.Combo > 0 &&
            GameProgressionSession.Combo % 3 == 0 &&
            BossAngerSession.ReduceAngerFromSuccess();
        DeviceHaptics.Play(HapticFeedbackType.Success);
        GameFeedbackOverlay.ShowTaskCompleted(
            awardedScore,
            bossCalmed,
            quality);
        ClearLaunchedTask();
        return true;
    }

    public static bool AbandonLaunchedTaskForScene(
        string sceneName)
    {
        if (!IsValidSlot(launchedSlotIndex) ||
            string.IsNullOrWhiteSpace(sceneName) ||
            !string.Equals(
                launchedSceneName,
                sceneName,
                StringComparison.Ordinal))
        {
            return false;
        }

        int slotIndex = launchedSlotIndex;
        ActiveTask launchedTask = activeTasks[slotIndex];
        if (launchedTask == null ||
            !string.Equals(
                launchedTask.taskId,
                launchedTaskId,
                StringComparison.Ordinal))
        {
            ClearLaunchedTask();
            return false;
        }

        if (GetRemainingTime(launchedTask) <= 0f)
        {
            return FailActiveTask(
                slotIndex,
                BossAngerFailureReason.TaskExpired);
        }

        ResumeTaskTimer(launchedTask);
        ClearLaunchedTask();
        return BossAngerSession.RegisterFailure(
            BossAngerFailureReason.TaskAbandoned);
    }

    public static bool TryConsumeCompletedTaskId(
        int slotIndex,
        out string taskId)
    {
        if (!IsValidSlot(slotIndex) ||
            string.IsNullOrWhiteSpace(completedTaskIds[slotIndex]))
        {
            taskId = null;
            return false;
        }

        taskId = completedTaskIds[slotIndex];
        completedTaskIds[slotIndex] = null;
        return true;
    }

    public static bool TryGetTask(
        int slotIndex,
        out TaskMissionSnapshot snapshot)
    {
        if (!IsValidSlot(slotIndex) ||
            activeTasks[slotIndex] == null)
        {
            snapshot = default;
            return false;
        }

        ActiveTask activeTask = activeTasks[slotIndex];
        float remainingTime = GetRemainingTime(activeTask);

        if (remainingTime <= 0f)
        {
            FailActiveTask(
                slotIndex,
                BossAngerFailureReason.TaskExpired);
            snapshot = default;
            return false;
        }

        snapshot = new TaskMissionSnapshot(
            activeTask.taskId,
            activeTask.route.SceneName,
            activeTask.route.Floor,
            activeTask.route.Room,
            remainingTime);
        return true;
    }

    public static bool IsRoomOccupied(int floor, int room)
    {
        for (int slotIndex = 0;
             slotIndex < activeTasks.Length;
             slotIndex++)
        {
            if (TryGetTask(slotIndex, out TaskMissionSnapshot task) &&
                task.Floor == floor &&
                task.Room == room)
            {
                return true;
            }
        }

        return false;
    }

    public static void PauseTimers()
    {
        if (timersPaused)
            return;

        timersPaused = true;
        timersPausedAt = ReadRealtimeNow();
    }

    public static void ResumeTimers()
    {
        if (!timersPaused)
            return;

        double pausedDuration = Math.Max(
            0d,
            ReadRealtimeNow() - timersPausedAt);
        for (int index = 0; index < activeTasks.Length; index++)
        {
            if (activeTasks[index] != null &&
                !activeTasks[index].timerPaused)
            {
                activeTasks[index].expiresAt += pausedDuration;
            }
        }

        timersPaused = false;
        timersPausedAt = 0d;
    }

    private static double GetTimerNow()
    {
        return timersPaused
            ? timersPausedAt
            : ReadRealtimeNow();
    }

    public static bool TryGetSceneForRoomButton(
        string buttonName,
        out string sceneName)
    {
        sceneName = null;
        if (GamePauseSession.IsPaused)
            return false;

        if (!TryParseRoomButtonName(
                buttonName,
                out int floor,
                out int room))
        {
            return false;
        }

        for (int slotIndex = 0;
             slotIndex < activeTasks.Length;
             slotIndex++)
        {
            if (!TryGetTask(slotIndex, out TaskMissionSnapshot task) ||
                task.Floor != floor ||
                task.Room != room)
            {
                continue;
            }

            sceneName = task.SceneName;
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            launchedSlotIndex = slotIndex;
            launchedTaskId = task.TaskId;
            launchedSceneName = sceneName;
            PauseTaskTimer(activeTasks[slotIndex]);
            MiniGamePerformanceSession.Begin(sceneName);
            return true;
        }

        return false;
    }

    public static bool TryParseRoomButtonName(
        string buttonName,
        out int floor,
        out int room)
    {
        floor = 0;
        room = 0;

        if (string.IsNullOrWhiteSpace(buttonName) ||
            !buttonName.StartsWith(
                "bt_",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int cursor = 3;
        if (!TryReadNumber(buttonName, ref cursor, out floor) ||
            cursor >= buttonName.Length ||
            buttonName[cursor] != '.')
        {
            floor = 0;
            return false;
        }

        cursor++;
        if (!TryReadNumber(buttonName, ref cursor, out room))
        {
            floor = 0;
            room = 0;
            return false;
        }

        return floor > 0 && room > 0;
    }

    private static bool TryReadNumber(
        string value,
        ref int cursor,
        out int number)
    {
        number = 0;
        int start = cursor;

        while (cursor < value.Length &&
               char.IsDigit(value[cursor]))
        {
            number =
                (number * 10) +
                (value[cursor] - '0');
            cursor++;
        }

        return cursor > start;
    }

    private static bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex < activeTasks.Length;
    }

    private static float GetRemainingTime(ActiveTask task)
    {
        if (task == null)
            return 0f;

        double timerNow = task.timerPaused
            ? task.timerPausedAt
            : GetTimerNow();
        return (float)(task.expiresAt - timerNow);
    }

    private static void PauseTaskTimer(ActiveTask task)
    {
        if (task == null || task.timerPaused)
            return;

        task.timerPaused = true;
        task.timerPausedAt = GetTimerNow();
    }

    private static void ResumeTaskTimer(ActiveTask task)
    {
        if (task == null || !task.timerPaused)
            return;

        double pausedDuration = Math.Max(
            0d,
            GetTimerNow() - task.timerPausedAt);
        task.expiresAt += pausedDuration;
        task.timerPaused = false;
        task.timerPausedAt = 0d;
    }

    private static double ReadRealtimeNow()
    {
#if UNITY_EDITOR
        if (timerNowOverrideEnabled)
            return timerNowOverride;
#endif
        return Time.realtimeSinceStartupAsDouble;
    }

#if UNITY_EDITOR
    internal static void SetTimerNowForTests(double timerNow)
    {
        timerNowOverrideEnabled = true;
        timerNowOverride = timerNow;
    }

    internal static void ClearTimerNowForTests()
    {
        timerNowOverrideEnabled = false;
        timerNowOverride = 0d;
    }

    internal static bool ResumeLaunchedTaskTimerForTests()
    {
        if (!IsValidSlot(launchedSlotIndex) ||
            activeTasks[launchedSlotIndex] == null)
        {
            return false;
        }

        ResumeTaskTimer(activeTasks[launchedSlotIndex]);
        ClearLaunchedTask();
        return true;
    }
#endif

    private static bool FailActiveTask(
        int slotIndex,
        BossAngerFailureReason reason)
    {
        if (!IsValidSlot(slotIndex) ||
            activeTasks[slotIndex] == null)
        {
            return false;
        }

        activeTasks[slotIndex] = null;
        completedTaskIds[slotIndex] = null;

        if (launchedSlotIndex == slotIndex)
            ClearLaunchedTask();

        BossAngerSession.RegisterFailure(reason);
        return true;
    }

    private static void ClearLaunchedTask()
    {
        MiniGamePerformanceSession.Cancel();
        launchedSlotIndex = -1;
        launchedTaskId = null;
        launchedSceneName = null;
    }
}
