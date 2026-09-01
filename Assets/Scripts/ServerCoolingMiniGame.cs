using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class ServerCoolingMiniGame : MonoBehaviour
{
    private enum CoolingTool
    {
        None,
        Coolant,
        Wrench,
        Airflow
    }

    [SerializeField] private SpriteRenderer[] problemFans;
    [SerializeField] private SpriteRenderer runningFanReference;
    [SerializeField] private SpriteRenderer coolingCanister;
    [SerializeField] private SpriteRenderer wrench;
    [SerializeField] private SpriteRenderer airflowArrow;
    [SerializeField] private SpriteRenderer heatWaves;
    [SerializeField] private SpriteRenderer snowflakeBurst;
    [SerializeField] private SpriteRenderer warningBeacon;
    [SerializeField] private TextMesh progressText;
    [SerializeField] private TextMesh hintText;
    [SerializeField] private string completionSceneName = "YeniOfis";

    private Camera gameCamera;
    private bool[] repairedFans;
    private CoolingTool selectedTool;
    private bool inputLocked;
    private bool completing;
    private int repairedCount;
    private float idleClock;
    private Vector3 beaconRestScale;

    private void Awake()
    {
        gameCamera = Camera.main;
        repairedFans = new bool[problemFans == null ? 0 : problemFans.Length];

        if (runningFanReference != null)
            runningFanReference.enabled = false;
        if (snowflakeBurst != null)
            snowflakeBurst.enabled = false;
        if (warningBeacon != null)
            beaconRestScale = warningBeacon.transform.localScale;

        RefreshText();
        RefreshToolColours();
    }

    private void Update()
    {
        AnimateIdle();

        if (MiniGamePresentationSession.IsInputBlocked ||
            GamePauseSession.IsPaused || inputLocked || completing ||
            !MiniGamePointerInput.WasPressed(out Vector2 screenPosition) ||
            !MiniGamePointerInput.TryGetWorldPosition(
                gameCamera,
                screenPosition,
                out Vector2 worldPosition))
        {
            return;
        }

        if (MiniGamePointerInput.IsNear(
                coolingCanister, worldPosition, 0.72f))
        {
            SelectTool(CoolingTool.Coolant, coolingCanister);
            return;
        }

        if (MiniGamePointerInput.IsNear(wrench, worldPosition, 0.72f))
        {
            SelectTool(CoolingTool.Wrench, wrench);
            return;
        }

        if (MiniGamePointerInput.IsNear(
                airflowArrow, worldPosition, 0.72f))
        {
            SelectTool(CoolingTool.Airflow, airflowArrow);
            return;
        }

        for (int index = 0; index < repairedFans.Length; index++)
        {
            if (!MiniGamePointerInput.IsNear(
                    problemFans[index], worldPosition, 0.78f))
            {
                continue;
            }

            TryRepairFan(index);
            return;
        }
    }

    private void AnimateIdle()
    {
        idleClock += Time.unscaledDeltaTime;
        if (warningBeacon != null && !completing)
        {
            float pulse = 1f + Mathf.Sin(idleClock * 7.5f) * 0.1f;
            warningBeacon.transform.localScale = beaconRestScale * pulse;
        }

        if (heatWaves != null && heatWaves.enabled)
        {
            Color color = heatWaves.color;
            color.a = 0.58f + Mathf.Sin(idleClock * 5f) * 0.25f;
            heatWaves.color = color;
        }

        for (int index = 0; index < repairedFans.Length; index++)
        {
            if (repairedFans[index] && problemFans[index] != null)
            {
                problemFans[index].transform.Rotate(
                    0f,
                    0f,
                    -185f * Time.unscaledDeltaTime);
            }
        }
    }

    private void SelectTool(CoolingTool tool, SpriteRenderer renderer)
    {
        selectedTool = tool;
        RefreshToolColours();
        ProceduralGameAudio.Play(GameSound.UiClick, 0.02f);
        if (renderer != null)
        {
            StartCoroutine(MiniGameJuice.PunchScale(
                renderer.transform,
                renderer.transform.localScale,
                0.16f,
                0.2f));
        }

        if (hintText != null)
        {
            hintText.text = tool switch
            {
                CoolingTool.Coolant => "SELECT THE RED FAN",
                CoolingTool.Wrench => "SELECT THE STOPPED FAN",
                CoolingTool.Airflow => "SELECT THE FROZEN FAN",
                _ => "SELECT A TOOL"
            };
        }
    }

    private void TryRepairFan(int fanIndex)
    {
        if (fanIndex < 0 || fanIndex >= repairedFans.Length ||
            repairedFans[fanIndex])
        {
            RegisterWrongAction(
                fanIndex >= 0 && fanIndex < problemFans.Length
                    ? problemFans[fanIndex]
                    : null);
            return;
        }

        CoolingTool requiredTool = fanIndex switch
        {
            0 => CoolingTool.Coolant,
            1 => CoolingTool.Wrench,
            _ => CoolingTool.Airflow
        };

        if (selectedTool != requiredTool)
        {
            RegisterWrongAction(problemFans[fanIndex]);
            if (hintText != null)
                hintText.text = "WRONG TOOL. CHECK THE FAN.";
            return;
        }

        repairedFans[fanIndex] = true;
        repairedCount++;
        StartCoroutine(RepairFan(fanIndex, requiredTool));
    }

    private IEnumerator RepairFan(int fanIndex, CoolingTool tool)
    {
        inputLocked = true;
        SpriteRenderer fan = problemFans[fanIndex];
        ProceduralGameAudio.Play(
            tool == CoolingTool.Coolant
                ? GameSound.ServerCool
                : GameSound.ServerFan,
            0.025f);

        if (snowflakeBurst != null &&
            (tool == CoolingTool.Coolant || tool == CoolingTool.Airflow))
        {
            snowflakeBurst.transform.position = fan.transform.position;
            Color effectColor = snowflakeBurst.color;
            effectColor.a = 1f;
            snowflakeBurst.color = effectColor;
            snowflakeBurst.enabled = true;
            Vector3 effectScale = snowflakeBurst.transform.localScale;
            yield return MiniGameJuice.PopIn(
                snowflakeBurst.transform,
                effectScale,
                0.2f,
                1.18f);
            StartCoroutine(MiniGameJuice.FadeSprite(
                snowflakeBurst,
                1f,
                0f,
                0.28f,
                true));
        }

        if (fan != null)
        {
            yield return MiniGameJuice.FlashColor(
                fan,
                new Color(0.18f, 0.95f, 1f, 1f),
                0.38f,
                2);
            if (runningFanReference != null)
                fan.sprite = runningFanReference.sprite;
            fan.color = Color.white;
        }

        if (fanIndex == 0 && heatWaves != null)
            heatWaves.enabled = false;

        selectedTool = CoolingTool.None;
        RefreshToolColours();
        RefreshText();

        if (repairedCount == repairedFans.Length)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            yield return CompleteCooling();
            yield break;
        }

        inputLocked = false;
    }

    private IEnumerator CompleteCooling()
    {
        completing = true;
        if (warningBeacon != null)
            warningBeacon.enabled = false;
        if (progressText != null)
            progressText.text = "TEMPERATURE NORMAL";
        if (hintText != null)
            hintText.text = "THE SERVERS CAN BREATHE AGAIN.";

        ProceduralGameAudio.Play(GameSound.ServerCool);
        TaskMissionSession.CompleteLaunchedTaskForScene(
            SceneManager.GetActiveScene().name);
        ProceduralGameAudio.Play(GameSound.TaskCompleted);
        yield return new WaitForSecondsRealtime(0.95f);
        SceneManager.LoadScene(completionSceneName);
    }

    private void RegisterWrongAction(SpriteRenderer target)
    {
        MiniGamePerformanceSession.RegisterMistake();
        ProceduralGameAudio.Play(GameSound.WrongAction, 0.025f);
        if (target != null)
        {
            StartCoroutine(MiniGameJuice.FlashColor(
                target,
                new Color(1f, 0.15f, 0.12f, 1f),
                0.25f,
                2));
        }
    }

    private void RefreshToolColours()
    {
        SetToolColour(coolingCanister, selectedTool == CoolingTool.Coolant);
        SetToolColour(wrench, selectedTool == CoolingTool.Wrench);
        SetToolColour(airflowArrow, selectedTool == CoolingTool.Airflow);
    }

    private static void SetToolColour(
        SpriteRenderer renderer,
        bool selected)
    {
        if (renderer == null)
            return;

        renderer.color = selected
            ? new Color(0.42f, 1f, 1f, 1f)
            : Color.white;
    }

    private void RefreshText()
    {
        int total = repairedFans == null ? 0 : repairedFans.Length;
        if (progressText != null)
            progressText.text = $"COOLING SYSTEMS  {repairedCount}/{total}";
        if (hintText != null && !completing)
            hintText.text = "SELECT A TOOL, THEN A FAN";
    }

#if UNITY_EDITOR
    public void ConfigureEditor(
        SpriteRenderer[] configuredProblemFans,
        SpriteRenderer configuredRunningFan,
        SpriteRenderer configuredCanister,
        SpriteRenderer configuredWrench,
        SpriteRenderer configuredAirflow,
        SpriteRenderer configuredHeatWaves,
        SpriteRenderer configuredSnowflake,
        SpriteRenderer configuredBeacon,
        TextMesh configuredProgress,
        TextMesh configuredHint)
    {
        problemFans = configuredProblemFans;
        runningFanReference = configuredRunningFan;
        coolingCanister = configuredCanister;
        wrench = configuredWrench;
        airflowArrow = configuredAirflow;
        heatWaves = configuredHeatWaves;
        snowflakeBurst = configuredSnowflake;
        warningBeacon = configuredBeacon;
        progressText = configuredProgress;
        hintText = configuredHint;
        completionSceneName = "YeniOfis";
    }
#endif
}
