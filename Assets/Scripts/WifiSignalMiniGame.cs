using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class WifiSignalMiniGame : MonoBehaviour
{
    [Serializable]
    public class DeskTarget
    {
        [SerializeField] private Transform dropPoint;
        [SerializeField] private SpriteRenderer noSignal;
        [SerializeField] private SpriteRenderer connectedSignal;
        [SerializeField, Min(0.1f)] private float acceptanceRadius = 2f;

        [NonSerialized] private bool completed;
        [NonSerialized] private Vector3 noSignalRestScale;
        [NonSerialized] private Vector3 connectedSignalRestScale;

        public Transform DropPoint => dropPoint;
        public float AcceptanceRadius => acceptanceRadius;
        public bool Completed => completed;
        public SpriteRenderer NoSignal => noSignal;
        public SpriteRenderer ConnectedSignal => connectedSignal;
        public Vector3 NoSignalRestScale => noSignalRestScale;
        public Vector3 ConnectedSignalRestScale => connectedSignalRestScale;

        public void CapturePresentationState()
        {
            if (noSignal != null)
            {
                noSignalRestScale = noSignal.transform.localScale;
            }

            if (connectedSignal != null)
            {
                connectedSignalRestScale = connectedSignal.transform.localScale;
            }
        }

        public void ResetState()
        {
            completed = false;

            if (noSignal != null)
            {
                noSignal.transform.localScale = noSignalRestScale;
                noSignal.color = Color.white;
                noSignal.enabled = true;
            }

            if (connectedSignal != null)
            {
                connectedSignal.transform.localScale = connectedSignalRestScale;
                connectedSignal.color = Color.white;
                connectedSignal.enabled = false;
            }
        }

        public void Complete()
        {
            completed = true;

            if (noSignal != null)
            {
                noSignal.enabled = false;
            }

            if (connectedSignal != null)
            {
                connectedSignal.enabled = true;
            }
        }

        public void Configure(
            Transform newDropPoint,
            SpriteRenderer newNoSignal,
            SpriteRenderer newConnectedSignal,
            float newAcceptanceRadius)
        {
            dropPoint = newDropPoint;
            noSignal = newNoSignal;
            connectedSignal = newConnectedSignal;
            acceptanceRadius = Mathf.Max(0.1f, newAcceptanceRadius);
            CapturePresentationState();
        }
    }

    [Header("Scene References")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Transform draggableDevice;
    [SerializeField] private SpriteRenderer draggableRenderer;
    [SerializeField] private DeskTarget[] deskTargets = Array.Empty<DeskTarget>();

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float pickupRadius = 2.1f;
    [SerializeField, Min(0f)] private float edgePadding = 0.4f;

    [Header("Completion")]
    [SerializeField] private string completionSceneName = "YeniOfis";
    [SerializeField, Min(0f)] private float completionDelay = 0.45f;

    private Vector3 startPosition;
    private Vector3 deviceRestScale;
    private Color deviceRestColor;
    private SpriteRenderer deviceAura;
    private Vector3 auraRestScale;
    private Quaternion auraRestRotation;
    private Vector3 cameraRestPosition;
    private Coroutine cameraShakeRoutine;
    private Vector3 dragOffset;
    private bool dragging;
    private bool returningDevice;
    private bool inputReady;
    private bool completionStarted;
    private int completedDeskCount;

    private void Awake()
    {
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }

        if (draggableDevice != null)
        {
            startPosition = draggableDevice.position;
        }

        CachePresentationReferences();
        ResetGame();
    }

    private IEnumerator Start()
    {
        yield return AnimateEntrance();
    }

    private void Update()
    {
        if (MiniGamePresentationSession.IsInputBlocked)
            return;

        if (completionStarted || returningDevice ||
            gameCamera == null || draggableDevice == null)
        {
            return;
        }

        ReadPointer(
            out Vector2 screenPosition,
            out bool pressed,
            out bool held,
            out bool released);

        Vector3 pointerWorld = ScreenToWorld(screenPosition);

        if (inputReady && pressed && IsDeviceUnderPointer(pointerWorld))
        {
            ProceduralGameAudio.Play(GameSound.CablePickup, 0.035f);
            dragging = true;
            dragOffset = draggableDevice.position - pointerWorld;
            StartCoroutine(MiniGameJuice.PunchScale(
                draggableDevice,
                deviceRestScale,
                0.12f,
                0.2f));

            if (deviceAura != null)
            {
                StartCoroutine(MiniGameJuice.FlashColor(
                    deviceAura,
                    new Color(0.4f, 0.9f, 1f),
                    0.24f,
                    2));
            }
        }

        if (dragging && held)
        {
            Vector3 desiredPosition = pointerWorld + dragOffset;
            draggableDevice.position = ClampToCamera(desiredPosition);
            AnimateAuraWhileDragging();
        }

        if (dragging && released)
        {
            dragging = false;
            ResetAuraPresentation();
            ResolveDrop();
        }
    }

    public void Configure(
        Camera newGameCamera,
        Transform newDraggableDevice,
        SpriteRenderer newDraggableRenderer,
        DeskTarget[] newDeskTargets,
        float newPickupRadius,
        float newEdgePadding,
        string newCompletionSceneName,
        float newCompletionDelay)
    {
        gameCamera = newGameCamera;
        draggableDevice = newDraggableDevice;
        draggableRenderer = newDraggableRenderer;
        deskTargets = newDeskTargets ?? Array.Empty<DeskTarget>();
        pickupRadius = Mathf.Max(0.1f, newPickupRadius);
        edgePadding = Mathf.Max(0f, newEdgePadding);
        completionSceneName = newCompletionSceneName;
        completionDelay = Mathf.Max(0f, newCompletionDelay);

        if (draggableDevice != null)
        {
            startPosition = draggableDevice.position;
        }

        CachePresentationReferences();
        if (Application.isPlaying)
        {
            ResetGame();
        }
    }

    private void ResetGame()
    {
        completedDeskCount = 0;
        dragging = false;
        returningDevice = false;
        inputReady = false;
        completionStarted = false;

        foreach (DeskTarget target in deskTargets)
        {
            target?.CapturePresentationState();
            target?.ResetState();
        }

        ReturnDeviceToStart();
    }

    private void ResolveDrop()
    {
        DeskTarget matchingTarget = null;
        float bestDistance = float.MaxValue;

        foreach (DeskTarget target in deskTargets)
        {
            if (target == null || target.Completed || target.DropPoint == null)
            {
                continue;
            }

            float distance = Vector2.Distance(
                draggableDevice.position,
                target.DropPoint.position);

            float difficultyRadiusMultiplier = Mathf.Max(
                0.68f,
                1f - GameProgressionSession.DifficultyLevel * 0.05f);
            float allowedRadius =
                target.AcceptanceRadius * difficultyRadiusMultiplier;
            if (distance <= allowedRadius && distance < bestDistance)
            {
                matchingTarget = target;
                bestDistance = distance;
            }
        }

        if (matchingTarget != null)
        {
            matchingTarget.Complete();
            completedDeskCount++;
            inputReady = false;
            returningDevice = true;
            StartCoroutine(AnimateSuccessfulDrop(matchingTarget));
            return;
        }

        inputReady = false;
        returningDevice = true;
        StartCoroutine(AnimateFailedDrop());
    }

    private IEnumerator AnimateEntrance()
    {
        if (draggableDevice == null)
        {
            yield break;
        }

        inputReady = false;
        StartCoroutine(MiniGameJuice.PopIn(
            draggableDevice,
            deviceRestScale,
            0.3f,
            1.18f));

        foreach (DeskTarget target in deskTargets)
        {
            if (target?.NoSignal == null)
            {
                continue;
            }

            StartCoroutine(MiniGameJuice.PopIn(
                target.NoSignal.transform,
                target.NoSignalRestScale,
                0.22f,
                1.16f));
            yield return new WaitForSecondsRealtime(0.045f);
        }

        yield return new WaitForSecondsRealtime(0.24f);
        inputReady = true;
    }

    private IEnumerator AnimateSuccessfulDrop(DeskTarget target)
    {
        Vector3 targetPosition = target.DropPoint.position;
        targetPosition.z = draggableDevice.position.z;

        yield return MiniGameJuice.MoveTransform(
            draggableDevice,
            draggableDevice.position,
            targetPosition,
            0.14f,
            0.08f);
        ProceduralGameAudio.Play(GameSound.WifiConnected, 0.035f);

        if (target.ConnectedSignal != null)
        {
            StartCoroutine(MiniGameJuice.PopIn(
                target.ConnectedSignal.transform,
                target.ConnectedSignalRestScale,
                0.24f,
                1.24f));
            StartCoroutine(MiniGameJuice.FlashColor(
                target.ConnectedSignal,
                new Color(0.45f, 1f, 0.62f),
                0.34f,
                2));
        }

        ShakeCamera(0.025f, 0.16f);
        yield return new WaitForSecondsRealtime(0.18f);
        yield return MiniGameJuice.MoveTransform(
            draggableDevice,
            draggableDevice.position,
            startPosition,
            0.28f,
            0.3f);

        RestoreDevicePresentation();
        returningDevice = false;

        if (completedDeskCount >= deskTargets.Length && deskTargets.Length > 0)
        {
            StartCoroutine(FinishGame());
        }
        else
        {
            inputReady = true;
        }
    }

    private IEnumerator AnimateFailedDrop()
    {
        MiniGamePerformanceSession.RegisterMistake();
        ProceduralGameAudio.Play(GameSound.WrongAction, 0.025f);
        Vector3 failedPosition = draggableDevice.position;
        if (draggableRenderer != null)
        {
            StartCoroutine(MiniGameJuice.FlashColor(
                draggableRenderer,
                new Color(1f, 0.18f, 0.16f),
                0.28f,
                2));
        }

        yield return MiniGameJuice.ShakePosition(
            draggableDevice,
            failedPosition,
            0.1f,
            0.24f,
            54f);
        yield return MiniGameJuice.MoveTransform(
            draggableDevice,
            draggableDevice.position,
            startPosition,
            0.3f,
            0.22f);

        RestoreDevicePresentation();
        returningDevice = false;
        inputReady = true;
    }

    private IEnumerator FinishGame()
    {
        completionStarted = true;
        inputReady = false;
        ProceduralGameAudio.Play(GameSound.TaskCompleted);

        foreach (DeskTarget target in deskTargets)
        {
            if (target?.ConnectedSignal == null)
            {
                continue;
            }

            StartCoroutine(MiniGameJuice.PunchScale(
                target.ConnectedSignal.transform,
                target.ConnectedSignalRestScale,
                0.12f,
                0.3f));
            yield return new WaitForSecondsRealtime(0.04f);
        }

        ShakeCamera(0.04f, 0.24f);

        if (completionDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(completionDelay);
        }

        if (string.IsNullOrWhiteSpace(completionSceneName))
        {
            Debug.LogError("Wi-Fi mini game completion scene is missing.", this);
            completionStarted = false;
            yield break;
        }

        TaskMissionSession.CompleteLaunchedTaskForScene(
            SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(completionSceneName);
    }

    private void ReturnDeviceToStart()
    {
        if (draggableDevice != null)
        {
            draggableDevice.position = startPosition;
        }
    }

    private void CachePresentationReferences()
    {
        if (draggableDevice == null)
        {
            return;
        }

        deviceRestScale = draggableDevice.localScale;
        deviceRestColor = draggableRenderer != null
            ? draggableRenderer.color
            : Color.white;

        SpriteRenderer[] renderers =
            draggableDevice.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != draggableRenderer)
            {
                deviceAura = renderer;
                auraRestScale = renderer.transform.localScale;
                auraRestRotation = renderer.transform.localRotation;
                break;
            }
        }

        if (gameCamera != null)
        {
            cameraRestPosition = gameCamera.transform.position;
        }
    }

    private void AnimateAuraWhileDragging()
    {
        if (deviceAura == null)
        {
            return;
        }

        float wave = Mathf.Sin(Time.unscaledTime * 7f);
        deviceAura.transform.localScale = auraRestScale *
            (1f + wave * 0.05f);
        deviceAura.transform.localRotation = auraRestRotation *
            Quaternion.Euler(0f, 0f, Time.unscaledTime * 25f);
    }

    private void ResetAuraPresentation()
    {
        if (deviceAura == null)
        {
            return;
        }

        deviceAura.transform.localScale = auraRestScale;
        deviceAura.transform.localRotation = auraRestRotation;
    }

    private void RestoreDevicePresentation()
    {
        ReturnDeviceToStart();
        draggableDevice.localScale = deviceRestScale;

        if (draggableRenderer != null)
        {
            draggableRenderer.color = deviceRestColor;
        }

        ResetAuraPresentation();
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
                52f));
    }

    private void OnDisable()
    {
        if (gameCamera != null)
        {
            gameCamera.transform.position = cameraRestPosition;
        }

        if (draggableDevice != null)
        {
            RestoreDevicePresentation();
        }
    }

    private bool IsDeviceUnderPointer(Vector3 pointerWorld)
    {
        if (Vector2.Distance(pointerWorld, draggableDevice.position) <= pickupRadius)
        {
            return true;
        }

        if (draggableRenderer == null || !draggableRenderer.enabled)
        {
            return false;
        }

        Bounds bounds = draggableRenderer.bounds;
        return bounds.Contains(new Vector3(pointerWorld.x, pointerWorld.y, bounds.center.z));
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        float distance = Mathf.Abs(
            gameCamera.transform.position.z - draggableDevice.position.z);

        Vector3 world = gameCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distance));
        world.z = draggableDevice.position.z;
        return world;
    }

    private Vector3 ClampToCamera(Vector3 position)
    {
        float halfHeight = gameCamera.orthographicSize - edgePadding;
        float halfWidth = gameCamera.orthographicSize * gameCamera.aspect - edgePadding;
        Vector3 cameraPosition = gameCamera.transform.position;

        position.x = Mathf.Clamp(
            position.x,
            cameraPosition.x - halfWidth,
            cameraPosition.x + halfWidth);
        position.y = Mathf.Clamp(
            position.y,
            cameraPosition.y - halfHeight,
            cameraPosition.y + halfHeight);
        position.z = draggableDevice.position.z;
        return position;
    }

    private static void ReadPointer(
        out Vector2 position,
        out bool pressed,
        out bool held,
        out bool released)
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            pressed = touch.press.wasPressedThisFrame;
            held = touch.press.isPressed;
            released = touch.press.wasReleasedThisFrame;

            if (pressed || held || released)
            {
                position = touch.position.ReadValue();
                return;
            }
        }

        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            pressed = Mouse.current.leftButton.wasPressedThisFrame;
            held = Mouse.current.leftButton.isPressed;
            released = Mouse.current.leftButton.wasReleasedThisFrame;
            return;
        }

        position = Vector2.zero;
        pressed = false;
        held = false;
        released = false;
    }
}
