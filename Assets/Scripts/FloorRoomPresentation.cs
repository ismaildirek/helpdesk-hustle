using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class FloorRoomPresentation : MonoBehaviour
{
    private sealed class RoomView
    {
        public Button Button;
        public RectTransform RectTransform;
        public Image Highlight;
        public Vector3 RestScale;
        public Vector2 RestAnchoredPosition;
        public Vector2 RestScreenPosition;
        public Vector3 WorldAnchor;
        public Canvas Canvas;
        public int Floor;
        public int Room;
        public bool HasActiveTask;
        public bool RestInteractable;
    }

    [Header("Room Focus")]
    [SerializeField, Range(0.4f, 0.9f)]
    private float focusZoomRatio = 0.62f;
    [SerializeField, Min(0.1f)] private float focusDuration = 0.32f;
    [SerializeField, Min(0.1f)] private float returnDuration = 0.24f;

    [Header("Active Task Highlight")]
    [SerializeField] private Color taskHighlightColor =
        new(0.1f, 0.82f, 1f, 1f);
    [SerializeField, Range(0f, 0.5f)] private float minimumHighlightAlpha = 0.06f;
    [SerializeField, Range(0f, 0.6f)] private float maximumHighlightAlpha = 0.2f;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 2.4f;

    private readonly List<RoomView> roomViews = new();
    private Camera sceneCamera;
    private PixelPerfectCamera pixelPerfectCamera;
    private NoTaskRoomFeedback noTaskFeedback;
    private Bounds backgroundBounds;
    private Vector3 cameraRestPosition;
    private float cameraRestSize;
    private float nextTaskRefreshTime;
    private bool hasBackgroundBounds;
    private bool initialized;
    private bool pixelPerfectWasEnabled;
    private bool pixelPerfectSuspended;
    private bool selectionRunning;

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (GamePauseSession.IsPaused)
            return;

        if (!initialized)
        {
            Initialize();
        }

        if (!initialized)
        {
            return;
        }

        if (Time.unscaledTime >= nextTaskRefreshTime)
        {
            RefreshActiveTasks();
            nextTaskRefreshTime = Time.unscaledTime + 0.35f;
        }

        AnimateTaskHighlights();
    }

    public void SelectRoom(string buttonName)
    {
        if (GamePauseSession.IsPaused)
            return;

        Initialize();

        if (!initialized || selectionRunning ||
            (noTaskFeedback != null &&
             !noTaskFeedback.CanAcceptRoomSelection))
        {
            return;
        }

        RoomView selectedView = FindRoomView(buttonName);
        if (selectedView == null)
        {
            ResolveRoomSelection(buttonName);
            return;
        }

        StartCoroutine(AnimateRoomSelection(selectedView));
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        sceneCamera = Camera.main;
        if (sceneCamera == null || !sceneCamera.orthographic)
        {
            Debug.LogWarning(
                "Floor room focus needs an orthographic Main Camera.",
                this);
            return;
        }

        noTaskFeedback = FindFirstObjectByType<NoTaskRoomFeedback>(
            FindObjectsInactive.Include);
        pixelPerfectCamera =
            sceneCamera.GetComponent<PixelPerfectCamera>();
        cameraRestPosition = sceneCamera.transform.position;
        cameraRestSize = sceneCamera.orthographicSize;
        FindBackgroundBounds();
        Canvas.ForceUpdateCanvases();

        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            if (button == null ||
                !TaskMissionSession.TryParseRoomButtonName(
                    button.gameObject.name,
                    out int floor,
                    out int room))
            {
                continue;
            }

            RoomView view = new()
            {
                Button = button,
                RectTransform = button.transform as RectTransform,
                RestScale = button.transform.localScale,
                Canvas = button.GetComponentInParent<Canvas>(),
                Floor = floor,
                Room = room,
                RestInteractable = button.interactable
            };

            CaptureRoomAlignment(view);

            view.Highlight = CreateHighlight(view);
            roomViews.Add(view);

            if (button.GetComponent<MiniGameLauncher>() == null)
            {
                string capturedName = button.gameObject.name;
                button.onClick.AddListener(() => SelectRoom(capturedName));
            }
        }

        initialized = roomViews.Count > 0;
        if (!initialized)
        {
            Debug.LogWarning("No floor room buttons were found.", this);
            return;
        }

        RefreshActiveTasks();
    }

    private Image CreateHighlight(RoomView view)
    {
        if (view.RectTransform == null)
        {
            return null;
        }

        Transform existing = view.RectTransform.Find("ActiveTaskPulse");
        GameObject highlightObject = existing != null
            ? existing.gameObject
            : new GameObject(
                "ActiveTaskPulse",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

        RectTransform rect = highlightObject.GetComponent<RectTransform>();
        rect.SetParent(view.RectTransform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();

        Image image = highlightObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(
            taskHighlightColor.r,
            taskHighlightColor.g,
            taskHighlightColor.b,
            0f);
        return image;
    }

    private IEnumerator AnimateRoomSelection(RoomView view)
    {
        selectionRunning = true;
        SetRoomButtonsInteractable(false);
        SuspendPixelPerfectCamera();

        Vector3 focusPosition = GetFocusPosition(view);
        float focusSize = Mathf.Max(
            1f,
            cameraRestSize * Mathf.Min(focusZoomRatio, 0.58f));
        float effectiveFocusDuration = Mathf.Max(0.38f, focusDuration);
        focusPosition = ClampToBackground(focusPosition, focusSize);

        if (view.Highlight != null)
        {
            view.Highlight.color = new Color(
                taskHighlightColor.r,
                taskHighlightColor.g,
                taskHighlightColor.b,
                0.34f);
        }

        if (view.Highlight != null)
        {
            StartCoroutine(MiniGameJuice.PunchScale(
                view.Highlight.rectTransform,
                Vector3.one,
                0.08f,
                effectiveFocusDuration));
        }

        yield return AnimateCamera(
            cameraRestPosition,
            focusPosition,
            cameraRestSize,
            focusSize,
            effectiveFocusDuration);

        yield return new WaitForSecondsRealtime(0.06f);

        if (TaskMissionSession.TryGetSceneForRoomButton(
                view.Button.gameObject.name,
                out string sceneName))
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        yield return AnimateCamera(
            focusPosition,
            cameraRestPosition,
            focusSize,
            cameraRestSize,
            returnDuration);

        RestoreRoomView(view);
        SetRoomButtonsInteractable(true);
        selectionRunning = false;
        RestorePixelPerfectCamera();
        noTaskFeedback?.ShowRandomFeedback();
    }

    private void SuspendPixelPerfectCamera()
    {
        if (pixelPerfectCamera == null || pixelPerfectSuspended)
        {
            return;
        }

        pixelPerfectWasEnabled = pixelPerfectCamera.enabled;
        pixelPerfectSuspended = true;

        if (pixelPerfectWasEnabled)
        {
            pixelPerfectCamera.enabled = false;
        }
    }

    private void RestorePixelPerfectCamera()
    {
        if (pixelPerfectCamera == null || !pixelPerfectSuspended)
        {
            return;
        }

        if (pixelPerfectWasEnabled)
        {
            pixelPerfectCamera.enabled = true;
        }

        pixelPerfectSuspended = false;
    }

    private IEnumerator AnimateCamera(
        Vector3 fromPosition,
        Vector3 toPosition,
        float fromSize,
        float toSize,
        float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration && sceneCamera != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = progress * progress * (3f - 2f * progress);
            sceneCamera.transform.position = Vector3.LerpUnclamped(
                fromPosition,
                toPosition,
                eased);
            sceneCamera.orthographicSize = Mathf.LerpUnclamped(
                fromSize,
                toSize,
                eased);
            SynchronizeRoomOverlays(sceneCamera.orthographicSize);
            yield return null;
        }

        if (sceneCamera != null)
        {
            sceneCamera.transform.position = toPosition;
            sceneCamera.orthographicSize = toSize;
            SynchronizeRoomOverlays(toSize);
        }
    }

    private void CaptureRoomAlignment(RoomView view)
    {
        if (view?.RectTransform == null || sceneCamera == null)
            return;

        view.RestAnchoredPosition = view.RectTransform.anchoredPosition;
        view.RestScreenPosition =
            RectTransformUtility.WorldToScreenPoint(
                null,
                view.RectTransform.position);
        float cameraDistance = Mathf.Abs(
            sceneCamera.transform.position.z);
        view.WorldAnchor = sceneCamera.ScreenToWorldPoint(
            new Vector3(
                view.RestScreenPosition.x,
                view.RestScreenPosition.y,
                cameraDistance));
    }

    private void SynchronizeRoomOverlays(float cameraSize)
    {
        if (sceneCamera == null || cameraRestSize <= 0f)
            return;

        float visualScale = cameraRestSize /
            Mathf.Max(0.01f, cameraSize);

        foreach (RoomView view in roomViews)
        {
            if (view?.RectTransform == null)
                continue;

            Vector2 currentScreenPosition =
                sceneCamera.WorldToScreenPoint(view.WorldAnchor);
            Vector2 screenDelta =
                currentScreenPosition - view.RestScreenPosition;
            float canvasScale = view.Canvas != null
                ? Mathf.Max(0.01f, view.Canvas.scaleFactor)
                : 1f;

            view.RectTransform.anchoredPosition =
                view.RestAnchoredPosition + screenDelta / canvasScale;
            view.RectTransform.localScale =
                view.RestScale * visualScale;
        }
    }

    private void ResolveRoomSelection(string buttonName)
    {
        if (TaskMissionSession.TryGetSceneForRoomButton(
                buttonName,
                out string sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        noTaskFeedback?.ShowRandomFeedback();
    }

    private Vector3 GetFocusPosition(RoomView view)
    {
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
            null,
            view.RectTransform.position);
        float cameraDistance = Mathf.Abs(
            sceneCamera.transform.position.z);
        Vector3 worldPosition = sceneCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                cameraDistance));
        worldPosition.z = cameraRestPosition.z;
        return worldPosition;
    }

    private void FindBackgroundBounds()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        float largestArea = 0f;

        foreach (SpriteRenderer renderer in renderers)
        {
            float area = renderer.bounds.size.x * renderer.bounds.size.y;
            if (area <= largestArea)
            {
                continue;
            }

            largestArea = area;
            backgroundBounds = renderer.bounds;
            hasBackgroundBounds = true;
        }
    }

    private Vector3 ClampToBackground(Vector3 position, float cameraSize)
    {
        if (!hasBackgroundBounds)
        {
            return position;
        }

        float halfHeight = cameraSize;
        float halfWidth = cameraSize * sceneCamera.aspect;
        float minimumX = backgroundBounds.min.x + halfWidth;
        float maximumX = backgroundBounds.max.x - halfWidth;
        float minimumY = backgroundBounds.min.y + halfHeight;
        float maximumY = backgroundBounds.max.y - halfHeight;

        position.x = minimumX <= maximumX
            ? Mathf.Clamp(position.x, minimumX, maximumX)
            : backgroundBounds.center.x;
        position.y = minimumY <= maximumY
            ? Mathf.Clamp(position.y, minimumY, maximumY)
            : backgroundBounds.center.y;
        position.z = cameraRestPosition.z;
        return position;
    }

    private void RefreshActiveTasks()
    {
        foreach (RoomView view in roomViews)
        {
            bool hasActiveTask = TaskMissionSession.IsRoomOccupied(
                view.Floor,
                view.Room);

            if (view.HasActiveTask == hasActiveTask)
                continue;

            view.HasActiveTask = hasActiveTask;
            if (!hasActiveTask)
            {
                if (view.Highlight != null)
                {
                    Color color = view.Highlight.color;
                    color.a = 0f;
                    view.Highlight.color = color;
                }

                if (view.RectTransform != null)
                    view.RectTransform.localScale = view.RestScale;
            }
        }
    }

    private void AnimateTaskHighlights()
    {
        float wave = 0.5f +
            0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);

        foreach (RoomView view in roomViews)
        {
            if (!view.HasActiveTask || selectionRunning)
                continue;

            if (view.Highlight != null)
            {
                float alpha = Mathf.Lerp(
                    minimumHighlightAlpha,
                    maximumHighlightAlpha,
                    wave);
                view.Highlight.color = new Color(
                    taskHighlightColor.r,
                    taskHighlightColor.g,
                    taskHighlightColor.b,
                    alpha);
            }

            if (view.RectTransform != null)
                view.RectTransform.localScale =
                    view.RestScale * (1f + wave * 0.015f);
        }
    }

    private RoomView FindRoomView(string buttonName)
    {
        foreach (RoomView view in roomViews)
        {
            if (view.Button != null &&
                string.Equals(
                    view.Button.gameObject.name,
                    buttonName,
                    StringComparison.Ordinal))
            {
                return view;
            }
        }

        return null;
    }

    private void SetRoomButtonsInteractable(bool interactable)
    {
        foreach (RoomView view in roomViews)
        {
            if (view.Button != null)
            {
                view.Button.interactable =
                    interactable && view.RestInteractable;
            }
        }
    }

    private void RestoreRoomView(RoomView view)
    {
        if (view.RectTransform != null)
        {
            view.RectTransform.anchoredPosition =
                view.RestAnchoredPosition;
            view.RectTransform.localScale = view.RestScale;
        }
    }

    private void OnDisable()
    {
        if (sceneCamera != null)
        {
            sceneCamera.transform.position = cameraRestPosition;
            sceneCamera.orthographicSize = cameraRestSize;
        }

        RestorePixelPerfectCamera();

        foreach (RoomView view in roomViews)
        {
            RestoreRoomView(view);

            if (view.Button != null)
            {
                view.Button.interactable = view.RestInteractable;
            }
        }
    }
}
