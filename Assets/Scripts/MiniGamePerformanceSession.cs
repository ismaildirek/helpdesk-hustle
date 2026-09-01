using System;
using UnityEngine;

internal enum TaskQuality
{
    Messy,
    Good,
    Perfect
}

internal readonly struct TaskQualityResult
{
    public TaskQualityResult(
        TaskQuality quality,
        int mistakeCount,
        float responseRatio,
        float scoreMultiplier)
    {
        Quality = quality;
        MistakeCount = Mathf.Max(0, mistakeCount);
        ResponseRatio = Mathf.Clamp01(responseRatio);
        ScoreMultiplier = Mathf.Max(0f, scoreMultiplier);
    }

    public TaskQuality Quality { get; }
    public int MistakeCount { get; }
    public float ResponseRatio { get; }
    public float ScoreMultiplier { get; }

    public string Label => Quality switch
    {
        TaskQuality.Perfect => "PERFECT!",
        TaskQuality.Good => "GOOD JOB!",
        _ => "MESSY, BUT DONE!"
    };
}

internal static class MiniGamePerformanceSession
{
    private const float PerfectResponseRatio = 0.55f;
    private const float GoodResponseRatio = 0.20f;
    private const int MaximumGoodMistakes = 2;
    private const float PerfectScoreMultiplier = 1.25f;
    private const float GoodScoreMultiplier = 1f;
    private const float MessyScoreMultiplier = 0.75f;

    private static string activeSceneName;
    private static int mistakeCount;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        Cancel();
    }

    public static void Begin(string sceneName)
    {
        activeSceneName = string.IsNullOrWhiteSpace(sceneName)
            ? null
            : sceneName;
        mistakeCount = 0;
    }

    public static void RegisterMistake()
    {
        if (string.IsNullOrEmpty(activeSceneName))
            return;

        mistakeCount++;
    }

    public static TaskQualityResult Complete(
        string sceneName,
        float remainingTime,
        float assignedDuration)
    {
        int registeredMistakes = string.Equals(
            activeSceneName,
            sceneName,
            StringComparison.Ordinal)
            ? mistakeCount
            : 0;
        TaskQualityResult result = Evaluate(
            remainingTime,
            assignedDuration,
            registeredMistakes);
        Cancel();
        return result;
    }

    public static void Cancel()
    {
        activeSceneName = null;
        mistakeCount = 0;
    }

    internal static TaskQualityResult Evaluate(
        float remainingTime,
        float assignedDuration,
        int registeredMistakes)
    {
        float safeDuration = Mathf.Max(0.01f, assignedDuration);
        float responseRatio = Mathf.Clamp01(
            Mathf.Max(0f, remainingTime) / safeDuration);
        int safeMistakeCount = Mathf.Max(0, registeredMistakes);

        if (safeMistakeCount == 0 &&
            responseRatio >= PerfectResponseRatio)
        {
            return new TaskQualityResult(
                TaskQuality.Perfect,
                safeMistakeCount,
                responseRatio,
                PerfectScoreMultiplier);
        }

        if (safeMistakeCount <= MaximumGoodMistakes &&
            responseRatio >= GoodResponseRatio)
        {
            return new TaskQualityResult(
                TaskQuality.Good,
                safeMistakeCount,
                responseRatio,
                GoodScoreMultiplier);
        }

        return new TaskQualityResult(
            TaskQuality.Messy,
            safeMistakeCount,
            responseRatio,
            MessyScoreMultiplier);
    }
}