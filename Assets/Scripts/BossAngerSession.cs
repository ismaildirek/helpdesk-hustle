using System;
using UnityEngine;

internal enum BossAngerFailureReason
{
    TaskExpired,
    TaskAbandoned
}

internal static class BossAngerSession
{
    public const int FailureCountBeforeLoss = 6;

    private static int failureCount;

    public static event Action Changed;

    public static int FailureCount => failureCount;
    public static int VisualStage => failureCount <= 0
        ? -1
        : Mathf.Clamp((failureCount - 1) / 2, 0, 2);
    public static float FillAmount => Mathf.Clamp01(
        failureCount / (float)FailureCountBeforeLoss);
    public static bool HasLost { get; private set; }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        failureCount = 0;
        HasLost = false;
        Changed = null;
    }

    public static void BeginNewGame()
    {
        failureCount = 0;
        HasLost = false;
        Changed?.Invoke();
    }

    public static bool RegisterFailure(
        BossAngerFailureReason reason)
    {
        if (HasLost)
            return false;

        int angerIncrease =
            DailyOfficeEventSession.AngerPerFailure;
        failureCount = Mathf.Min(
            failureCount + angerIncrease,
            FailureCountBeforeLoss);
        HasLost = failureCount >= FailureCountBeforeLoss;

        Debug.Log(
            $"Boss anger increased: {reason} " +
            $"(+{angerIncrease}, " +
            $"{failureCount}/{FailureCountBeforeLoss}).");

        GameProgressionSession.RegisterTaskFailed();
        GameFeedbackOverlay.ShowTaskFailed();

        Changed?.Invoke();

        if (HasLost)
        {
            GameProgressionSession.FinalizeRun();
            BossAngerLossRouter.ScheduleReturnToEntrance();
        }

        return true;
    }

    public static bool ReduceAngerFromSuccess()
    {
        if (HasLost || failureCount <= 0)
            return false;

        failureCount--;
        Changed?.Invoke();
        return true;
    }
}
