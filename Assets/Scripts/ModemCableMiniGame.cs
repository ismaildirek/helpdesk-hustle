using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ModemCableMiniGame : MonoBehaviour
{
    private enum GameState
    {
        Aiming,
        Launching,
        Missed,
        Resetting,
        Completed
    }

    [Header("Scene References")]
    [SerializeField] private SpriteRenderer cableRenderer;
    [SerializeField] private SpriteRenderer modemRenderer;
    [SerializeField] private Camera gameCamera;

    [Header("Movement")]
    [SerializeField] private float horizontalSpeed = 0.9f;
    [SerializeField] private float launchSpeed = 2.8f;
    [SerializeField] private float verticalBobAmount = 0.08f;
    [SerializeField] private float verticalBobSpeed = 2.5f;
    [SerializeField] private float horizontalScreenPadding = 0.08f;

    [Header("Blue Port")]
    [SerializeField, Range(0f, 1f)] private float portNormalizedX = 0.67f;
    [SerializeField, Range(0f, 1f)] private float portNormalizedY = 0.5f;
    [SerializeField, Min(0.01f)] private float portHorizontalTolerance = 0.1f;
    [SerializeField, Range(0.01f, 0.05f)]
    private float maximumPortWidthFraction = 0.026f;

    [Header("Cable Plug Tip")]
    [SerializeField, Range(0f, 1f)]
    private float cableTipNormalizedX = 0.926f;
    [SerializeField, Range(0f, 1f)]
    private float cableTipNormalizedY = 0.532f;

    [Header("Completion")]
    [SerializeField] private float missPause = 0.45f;
    [SerializeField] private float completionPause = 0.9f;
    [SerializeField] private string completionSceneName = "YeniOfis";

    private GameState state;
    private Vector3 cableStartPosition;
    private Vector3 cableStartScale;
    private Quaternion cableStartRotation;
    private Color cableStartColor;
    private Vector3 modemRestScale;
    private Color modemStartColor;
    private Vector3 cameraRestPosition;
    private Coroutine cameraShakeRoutine;
    private float minCableX;
    private float maxCableX;
    private float stateTimer;
    private bool inputReady;

    public void Configure(
        SpriteRenderer cable,
        SpriteRenderer modem,
        Camera targetCamera)
    {
        cableRenderer = cable;
        modemRenderer = modem;
        gameCamera = targetCamera;
    }

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        int difficulty = GameProgressionSession.DifficultyLevel;
        horizontalSpeed *= 1f + Mathf.Min(0.55f, difficulty * 0.08f);
        portHorizontalTolerance *= Mathf.Max(
            0.65f,
            Mathf.Pow(0.95f, difficulty));

        if (cableRenderer == null)
            cableRenderer = FindRenderer("kablo");

        if (modemRenderer == null)
            modemRenderer = FindRenderer("modem");

        if (cableRenderer == null || modemRenderer == null || gameCamera == null)
        {
            Debug.LogError(
                "Modem mini game needs kablo, modem and Main Camera objects.",
                this);
            enabled = false;
            return;
        }

        cableStartPosition = cableRenderer.transform.position;
        cableStartScale = cableRenderer.transform.localScale;
        cableStartRotation = cableRenderer.transform.localRotation;
        cableStartColor = cableRenderer.color;
        modemRestScale = modemRenderer.transform.localScale;
        modemStartColor = modemRenderer.color;
        cameraRestPosition = gameCamera.transform.position;
        CalculateHorizontalLimits();
        ResetCable();
        inputReady = false;
        StartCoroutine(AnimateEntrance());
    }

    private void Update()
    {
        if (MiniGamePresentationSession.IsInputBlocked)
            return;

        switch (state)
        {
            case GameState.Aiming:
                UpdateAiming();
                break;
            case GameState.Launching:
                UpdateLaunch();
                break;
            case GameState.Missed:
                UpdateMiss();
                break;
            case GameState.Resetting:
                break;
            case GameState.Completed:
                UpdateCompletion();
                break;
        }
    }

    private void UpdateAiming()
    {
        float width = maxCableX - minCableX;
        float x = minCableX +
                  Mathf.PingPong(Time.time * horizontalSpeed, width);
        float y = cableStartPosition.y +
                  Mathf.Sin(Time.time * verticalBobSpeed) * verticalBobAmount;

        cableRenderer.transform.position =
            new Vector3(x, y, cableStartPosition.z);

        if (inputReady && WasPointerPressed())
        {
            ProceduralGameAudio.Play(GameSound.CablePickup, 0.025f);
            inputReady = false;
            state = GameState.Launching;
            StartCoroutine(MiniGameJuice.FlashColor(
                cableRenderer,
                new Color(0.35f, 0.85f, 1f),
                0.22f,
                1));
        }
    }

    private void UpdateLaunch()
    {
        cableRenderer.transform.position +=
            Vector3.up * (launchSpeed * Time.deltaTime);

        Vector2 tip = GetCableTipWorldPosition();
        Vector2 port = GetBluePortWorldPosition();
        if (tip.y < port.y)
            return;

        float allowedHorizontalError = GetAllowedHorizontalError();
        if (Mathf.Abs(tip.x - port.x) <= allowedHorizontalError)
        {
            CompleteConnection(tip, port);
            return;
        }

        cableRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
        MiniGamePerformanceSession.RegisterMistake();
        ProceduralGameAudio.Play(GameSound.WrongAction, 0.025f);
        stateTimer = missPause;
        state = GameState.Missed;
        StartCoroutine(AnimateMissFeedback());
    }

    private void UpdateMiss()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            state = GameState.Resetting;
            StartCoroutine(AnimateCableReset());
        }
    }

    private void UpdateCompletion()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f &&
            !string.IsNullOrWhiteSpace(completionSceneName))
            SceneManager.LoadScene(completionSceneName);
    }

    private void CompleteConnection(Vector2 tip, Vector2 port)
    {
        ProceduralGameAudio.Play(GameSound.ModemPlug, 0.02f);
        Vector3 position = cableRenderer.transform.position;
        position += new Vector3(port.x - tip.x, port.y - tip.y, 0f);
        cableRenderer.transform.position = position;
        cableRenderer.color = cableStartColor;
        inputReady = false;
        TaskMissionSession.CompleteLaunchedTaskForScene(
            SceneManager.GetActiveScene().name);
        stateTimer = completionPause;
        state = GameState.Completed;
        StartCoroutine(AnimateConnectionSuccess());
    }

    private void ResetCable()
    {
        cableRenderer.transform.position = cableStartPosition;
        cableRenderer.transform.localScale = cableStartScale;
        cableRenderer.transform.localRotation = cableStartRotation;
        cableRenderer.color = cableStartColor;
        cableRenderer.enabled = true;
        state = GameState.Aiming;
    }

    private IEnumerator AnimateEntrance()
    {
        StartCoroutine(MiniGameJuice.FadeSprite(
            modemRenderer,
            0f,
            modemStartColor.a,
            0.34f));
        yield return MiniGameJuice.PopIn(
            cableRenderer.transform,
            cableStartScale,
            0.3f,
            1.18f);
        inputReady = true;
    }

    private IEnumerator AnimateMissFeedback()
    {
        Vector3 missedPosition = cableRenderer.transform.position;
        StartCoroutine(MiniGameJuice.FlashColor(
            cableRenderer,
            Color.white,
            0.28f,
            2));
        ShakeCamera(0.025f, 0.16f);
        yield return MiniGameJuice.ShakePosition(
            cableRenderer.transform,
            missedPosition,
            0.07f,
            0.28f,
            55f);
    }

    private IEnumerator AnimateCableReset()
    {
        yield return MiniGameJuice.MoveScaleFade(
            cableRenderer,
            cableRenderer.transform.position,
            cableStartPosition,
            cableRenderer.transform.localScale,
            cableStartScale * 0.72f,
            0.24f);

        cableRenderer.enabled = true;
        cableRenderer.transform.position = cableStartPosition;
        cableRenderer.transform.localRotation = cableStartRotation;
        cableRenderer.transform.localScale = cableStartScale;
        cableRenderer.color = cableStartColor;

        yield return MiniGameJuice.PopIn(
            cableRenderer.transform,
            cableStartScale,
            0.18f,
            1.14f);

        inputReady = true;
        state = GameState.Aiming;
    }

    private IEnumerator AnimateConnectionSuccess()
    {
        StartCoroutine(MiniGameJuice.FlashColor(
            cableRenderer,
            new Color(0.45f, 1f, 0.58f),
            0.42f,
            2));
        StartCoroutine(MiniGameJuice.FlashColor(
            modemRenderer,
            new Color(0.5f, 1f, 0.65f),
            0.42f,
            2));
        StartCoroutine(MiniGameJuice.PunchScale(
            cableRenderer.transform,
            cableStartScale,
            0.15f,
            0.34f));
        ShakeCamera(0.045f, 0.24f);
        yield return new WaitForSecondsRealtime(0.12f);
        ProceduralGameAudio.Play(GameSound.TaskCompleted);
        yield return MiniGameJuice.PunchScale(
            modemRenderer.transform,
            modemRestScale,
            0.08f,
            0.36f);
    }

    private void ShakeCamera(float strength, float duration)
    {
        if (gameCamera == null)
        {
            return;
        }

        if (cameraShakeRoutine != null)
        {
            StopCoroutine(cameraShakeRoutine);
            gameCamera.transform.position = cameraRestPosition;
        }

        cameraShakeRoutine = StartCoroutine(
            MiniGameJuice.ShakePosition(
                gameCamera.transform,
                cameraRestPosition,
                strength,
                duration,
                54f));
    }

    private void OnDisable()
    {
        if (gameCamera != null)
        {
            gameCamera.transform.position = cameraRestPosition;
        }

        if (cableRenderer != null)
        {
            cableRenderer.transform.position = cableStartPosition;
            cableRenderer.transform.localScale = cableStartScale;
            cableRenderer.transform.localRotation = cableStartRotation;
            cableRenderer.color = cableStartColor;
        }

        if (modemRenderer != null)
        {
            modemRenderer.transform.localScale = modemRestScale;
            modemRenderer.color = modemStartColor;
        }
    }

    private void CalculateHorizontalLimits()
    {
        float cameraDistance = Mathf.Abs(
            gameCamera.transform.position.z -
            cableRenderer.transform.position.z);
        float left = gameCamera.ViewportToWorldPoint(
            new Vector3(horizontalScreenPadding, 0.5f, cameraDistance)).x;
        float right = gameCamera.ViewportToWorldPoint(
            new Vector3(1f - horizontalScreenPadding, 0.5f, cameraDistance)).x;
        float halfVisibleWidth = Mathf.Min(
            cableRenderer.bounds.extents.x,
            0.5f);

        minCableX = left + halfVisibleWidth;
        maxCableX = right - halfVisibleWidth;
    }

    private Vector2 GetBluePortWorldPosition()
    {
        Bounds spriteBounds = modemRenderer.sprite.bounds;
        Vector3 localPort = new(
            Mathf.Lerp(
                spriteBounds.min.x,
                spriteBounds.max.x,
                portNormalizedX),
            Mathf.Lerp(
                spriteBounds.min.y,
                spriteBounds.max.y,
                portNormalizedY),
            0f);

        return modemRenderer.transform.TransformPoint(localPort);
    }

    private float GetAllowedHorizontalError()
    {
        // The value in the Inspector remains tunable, but can never grow
        // wider than the physical blue socket in the modem artwork.
        float physicalPortLimit =
            modemRenderer.bounds.size.x * maximumPortWidthFraction;
        return Mathf.Min(portHorizontalTolerance, physicalPortLimit);
    }

    private Vector2 GetCableTipWorldPosition()
    {
        Bounds spriteBounds = cableRenderer.sprite.bounds;
        Vector3 localTip = new(
            Mathf.Lerp(
                spriteBounds.min.x,
                spriteBounds.max.x,
                cableTipNormalizedX),
            Mathf.Lerp(
                spriteBounds.min.y,
                spriteBounds.max.y,
                cableTipNormalizedY),
            0f);

        return cableRenderer.transform.TransformPoint(localTip);
    }

    private void OnDrawGizmosSelected()
    {
        if (modemRenderer == null)
            return;

        Vector2 port = GetBluePortWorldPosition();
        float tolerance = GetAllowedHorizontalError();

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(port, 0.08f);
        Gizmos.DrawLine(
            new Vector3(port.x - tolerance, port.y - 0.15f, 0f),
            new Vector3(port.x - tolerance, port.y + 0.15f, 0f));
        Gizmos.DrawLine(
            new Vector3(port.x + tolerance, port.y - 0.15f, 0f),
            new Vector3(port.x + tolerance, port.y + 0.15f, 0f));
    }

    private static bool WasPointerPressed()
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
    }

    private static SpriteRenderer FindRenderer(string objectName)
    {
        SpriteRenderer[] renderers =
            FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.name.Trim().Equals(
                    objectName,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return renderer;
            }
        }

        return null;
    }

#if false
    private void OnGUI()
    {
        if (!enabled)
            return;

        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Screen.height * 0.035f),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        instructionStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Screen.height * 0.025f),
            normal = { textColor = Color.white }
        };

        string title = state == GameState.Completed
            ? "BAĞLANTI BAŞARILI!"
            : state == GameState.Missed
                ? "YANLIŞ PORT!"
                : "MODEM KABLOSUNU BAĞLA";
        string instruction = state == GameState.Aiming
            ? "Mavi portun hizasına gelince ekrana dokun."
            : state == GameState.Launching
                ? "Kablo gönderiliyor..."
                : state == GameState.Missed
                    ? "Tekrar dene."
                    : "Yeni ofise dönülüyor...";

        GUI.Box(
            new Rect(Screen.width * 0.2f, 14f, Screen.width * 0.6f, 84f),
            GUIContent.none);
        GUI.Label(
            new Rect(Screen.width * 0.2f, 18f, Screen.width * 0.6f, 38f),
            title,
            titleStyle);
        GUI.Label(
            new Rect(Screen.width * 0.2f, 54f, Screen.width * 0.6f, 34f),
            instruction,
            instructionStyle);
    }
#endif
}
