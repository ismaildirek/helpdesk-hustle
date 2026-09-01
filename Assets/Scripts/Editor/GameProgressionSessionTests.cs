using System.IO;
using NUnit.Framework;
using UnityEditor;

public sealed class GameProgressionSessionTests
{
    [SetUp]
    public void SetUp()
    {
        DailyOfficeEventSession.BeginNewGame();
        GameProgressionSession.BeginNewGame();
        BossAngerSession.BeginNewGame();
        GamePauseSession.BeginNewGame();
        TaskMissionSession.BeginNewGame();
    }

    [TearDown]
    public void TearDown()
    {
        TaskMissionSession.BeginNewGame();
        TaskMissionSession.ClearTimerNowForTests();
        BossAngerSession.BeginNewGame();
    }

    [Test]
    public void CompletingTaskAwardsBaseAndRemainingTimeScore()
    {
        int awarded = GameProgressionSession.RegisterTaskCompleted(10f);

        Assert.That(awarded, Is.EqualTo(130));
        Assert.That(GameProgressionSession.Score, Is.EqualTo(130));
        Assert.That(GameProgressionSession.Combo, Is.EqualTo(1));
        Assert.That(GameProgressionSession.CompletedTasks, Is.EqualTo(1));
    }

    [Test]
    public void ThirdConsecutiveTaskReceivesComboTierBonus()
    {
        GameProgressionSession.RegisterTaskCompleted(0f);
        GameProgressionSession.RegisterTaskCompleted(0f);
        int thirdAward =
            GameProgressionSession.RegisterTaskCompleted(0f);

        Assert.That(thirdAward, Is.EqualTo(115));
        Assert.That(GameProgressionSession.Score, Is.EqualTo(315));
        Assert.That(GameProgressionSession.Combo, Is.EqualTo(3));
        Assert.That(GameProgressionSession.HighestCombo, Is.EqualTo(3));
    }

    [Test]
    public void FailedTaskResetsComboAndTracksFailure()
    {
        GameProgressionSession.RegisterTaskCompleted(0f);
        GameProgressionSession.RegisterTaskCompleted(0f);

        GameProgressionSession.RegisterTaskFailed();

        Assert.That(GameProgressionSession.Combo, Is.Zero);
        Assert.That(GameProgressionSession.FailedTasks, Is.EqualTo(1));
        Assert.That(GameProgressionSession.HighestCombo, Is.EqualTo(2));
    }

    [Test]
    public void DailyEventSelectionDoesNotRepeatPreviousEvent()
    {
        DailyOfficeEventType previous =
            DailyOfficeEventType.NetworkTrouble;

        for (int index = 0; index < 8; index++)
        {
            DailyOfficeEventType selected =
                DailyOfficeEventSession.SelectNextEvent(previous, index);
            Assert.That(selected, Is.Not.EqualTo(previous));
            Assert.That(selected, Is.Not.EqualTo(DailyOfficeEventType.None));
        }
    }

    [Test]
    public void BossInspectionAppliesRiskAndRewardModifiers()
    {
        DailyOfficeEventSession.ActivateForTests(
            DailyOfficeEventType.BossInspection);

        int awarded = GameProgressionSession.RegisterTaskCompleted(0f);

        Assert.That(
            DailyOfficeEventSession.TaskDurationMultiplier,
            Is.EqualTo(1f).Within(0.001f));
        Assert.That(DailyOfficeEventSession.AngerPerFailure, Is.EqualTo(2));
        Assert.That(awarded, Is.EqualTo(125));
    }

    [Test]
    public void CoffeeBoostAddsTaskTimeAndScore()
    {
        DailyOfficeEventSession.ActivateForTests(
            DailyOfficeEventType.CoffeeBoost);

        int awarded = GameProgressionSession.RegisterTaskCompleted(0f);

        Assert.That(
            DailyOfficeEventSession.TaskDurationMultiplier,
            Is.EqualTo(1.15f).Within(0.001f));
        Assert.That(awarded, Is.EqualTo(110));
    }

    [Test]
    public void SceneSavingEditorToolsDoNotAutoRunAfterReload()
    {
        string[] scriptGuids = AssetDatabase.FindAssets(
            "t:Script",
            new[] { "Assets/Scripts/Editor" });

        foreach (string scriptGuid in scriptGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(scriptGuid);
            if (path.EndsWith("Tests.cs"))
                continue;

            string source = File.ReadAllText(path);
            if (!source.Contains("SaveScene("))
                continue;

            Assert.That(
                source,
                Does.Not.Contain("[InitializeOnLoad]"),
                $"Scene-saving tool auto-runs: {path}");
            Assert.That(
                source,
                Does.Not.Contain("[DidReloadScripts]"),
                $"Scene-saving tool reloads automatically: {path}");
        }
    }

    [Test]
    public void TaskCardDescriptionRemovesBlankLines()
    {
        string formatted = TaskCardMissionBoard.NormalizeDescription(
            "PC parts are loose.\n\n  Tiny chaos inside.  ");

        Assert.That(
            formatted,
            Is.EqualTo("PC parts are loose.\nTiny chaos inside."));
    }

    [Test]
    public void EnteredTaskTimerFreezesWhileOtherTaskContinues()
    {
        TaskMissionSession.SetTimerNowForTests(100d);
        TaskMissionSession.EnsureSlotCount(2);
        TaskMissionSession.AssignTask(
            0,
            "cable_game",
            new TaskMissionRoute("kablo_game", 1, 3),
            30f);
        TaskMissionSession.AssignTask(
            1,
            "file_upload",
            new TaskMissionRoute("Dosya_Y\u00FCkle", 2, 4),
            30f);

        bool launched = TaskMissionSession.TryGetSceneForRoomButton(
            "bt_1.3",
            out string sceneName);
        TaskMissionSession.SetTimerNowForTests(112d);

        Assert.That(launched, Is.True);
        Assert.That(sceneName, Is.EqualTo("kablo_game"));
        Assert.That(
            TaskMissionSession.TryGetTask(0, out TaskMissionSnapshot entered),
            Is.True);
        Assert.That(
            TaskMissionSession.TryGetTask(1, out TaskMissionSnapshot waiting),
            Is.True);
        Assert.That(entered.RemainingTime, Is.EqualTo(30f).Within(0.001f));
        Assert.That(waiting.RemainingTime, Is.EqualTo(18f).Within(0.001f));
    }

    [Test]
    public void LeavingUnfinishedTaskResumesItsTimer()
    {
        TaskMissionSession.SetTimerNowForTests(100d);
        TaskMissionSession.EnsureSlotCount(1);
        TaskMissionSession.AssignTask(
            0,
            "cable_game",
            new TaskMissionRoute("kablo_game", 1, 3),
            30f);
        Assert.That(
            TaskMissionSession.TryGetSceneForRoomButton(
                "bt_1.3",
                out _),
            Is.True);

        TaskMissionSession.SetTimerNowForTests(112d);
        Assert.That(
            TaskMissionSession.ResumeLaunchedTaskTimerForTests(),
            Is.True);
        TaskMissionSession.SetTimerNowForTests(117d);

        Assert.That(
            TaskMissionSession.TryGetTask(0, out TaskMissionSnapshot task),
            Is.True);
        Assert.That(task.RemainingTime, Is.EqualTo(25f).Within(0.001f));
    }

    [Test]
    public void SurvivalClockWaitsForBossIntroToFinish()
    {
        SurvivalTimeSession.BeginNewGame();

        SurvivalTimeSession.Tick(8f, true);
        Assert.That(SurvivalTimeSession.ElapsedSeconds, Is.Zero);

        SurvivalTimeSession.Tick(2f, false);
        Assert.That(
            SurvivalTimeSession.ElapsedSeconds,
            Is.EqualTo(2f).Within(0.001f));
    }

    [TestCase("server_cooling", "Server_Cooling", 4, 1)]
    [TestCase("security_check", "Security_check", 3, 4)]
    public void EquipmentTaskRoutesMatchTheirRooms(
        string taskId,
        string expectedScene,
        int expectedFloor,
        int expectedRoom)
    {
        Assert.That(
            TaskMissionRouteCatalog.TryGetRoute(
                taskId,
                out TaskMissionRoute route),
            Is.True);
        Assert.That(route.SceneName, Is.EqualTo(expectedScene));
        Assert.That(route.Floor, Is.EqualTo(expectedFloor));
        Assert.That(route.Room, Is.EqualTo(expectedRoom));
    }

    [TestCase("katlar", "YeniOfis")]
    [TestCase("kablo_game", "katlar")]
    [TestCase("popup_ads", "katlar")]
    [TestCase("Giris_Ekran", "")]
    [TestCase("YeniOfis", "")]
    public void MobileBackDestinationMatchesSceneFlow(
        string activeScene,
        string expectedDestination)
    {
        Assert.That(
            MobileReleaseRuntime.GetBackDestination(activeScene),
            Is.EqualTo(expectedDestination));
    }
    [TestCase(80f, 100f, 0, 2, 1.25f)]
    [TestCase(60f, 100f, 1, 1, 1f)]
    [TestCase(15f, 100f, 0, 0, 0.75f)]
    [TestCase(90f, 100f, 3, 0, 0.75f)]
    public void TaskQualityUsesResponseTimeAndMistakes(
        float remainingTime,
        float duration,
        int mistakes,
        int expectedQuality,
        float expectedMultiplier)
    {
        TaskQualityResult result = MiniGamePerformanceSession.Evaluate(
            remainingTime,
            duration,
            mistakes);

        Assert.That((int)result.Quality, Is.EqualTo(expectedQuality));
        Assert.That(
            result.ScoreMultiplier,
            Is.EqualTo(expectedMultiplier).Within(0.001f));
        Assert.That(result.MistakeCount, Is.EqualTo(mistakes));
    }

    [Test]
    public void PerformanceSessionCountsOnlyActiveMiniGameMistakes()
    {
        MiniGamePerformanceSession.RegisterMistake();
        MiniGamePerformanceSession.Begin("kablo_game");
        MiniGamePerformanceSession.RegisterMistake();
        MiniGamePerformanceSession.RegisterMistake();

        TaskQualityResult result = MiniGamePerformanceSession.Complete(
            "kablo_game",
            30f,
            40f);

        Assert.That(result.Quality, Is.EqualTo(TaskQuality.Good));
        Assert.That(result.MistakeCount, Is.EqualTo(2));
    }

    [Test]
    public void PerfectQualityMultiplierIncreasesTaskScore()
    {
        int awarded = GameProgressionSession.RegisterTaskCompleted(
            12f,
            1.25f);

        Assert.That(awarded, Is.EqualTo(170));
        Assert.That(GameProgressionSession.Score, Is.EqualTo(170));
    }
}
