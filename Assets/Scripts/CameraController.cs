/*using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float minZoom = 2f;
    public float maxZoom = 15f;
    public float zoomSpeedPC = 0.005f; 
    public float zoomSpeedMobile = 0.005f;
    public float smoothZoomSpeed = 10f;
    [Tooltip("If true, prevents zooming out so much that the background is visible outside the bounds.")]
    public bool preventBackgroundVisible = true;

    [Header("Pan Settings")]
    public float panSmoothSpeed = 10f;

    [Header("Bounds Settings")]
    public bool useBounds = true;
    [Tooltip("If true, automatically calculates bounds based on all sprites in the scene.")]
    public bool autoCalculateBounds = false; 
    public Vector2 minBounds = new Vector2(-5f, -5f);
    public Vector2 maxBounds = new Vector2(5f, 5f);

    private Camera cam;
    private float targetZoom;
    private Vector3 targetPosition;
    private Vector2 dragOrigin;
    private bool isDragging;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        targetZoom = cam.orthographicSize;
        targetPosition = transform.position;

        if (useBounds && autoCalculateBounds)
        {
            CalculateBounds();
        }
        
        EnforceZoomLimit();
    }

    public void CalculateBounds()
    {
        SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers)
        {
            b.Encapsulate(r.bounds);
        }

        minBounds = new Vector2(b.min.x, b.min.y);
        maxBounds = new Vector2(b.max.x, b.max.y);
    }

    void Update()
    {
        HandleZoom();
        HandlePan();
        
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * smoothZoomSpeed);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * panSmoothSpeed);
        
        if (useBounds)
        {
            smoothedPosition = ClampCamera(smoothedPosition, cam.orthographicSize);
            targetPosition = ClampCamera(targetPosition, targetZoom);
        }
        
        transform.position = smoothedPosition;
    }

    void EnforceZoomLimit()
    {
        if (useBounds && preventBackgroundVisible)
        {
            float boundsWidth = maxBounds.x - minBounds.x;
            float boundsHeight = maxBounds.y - minBounds.y;
            
            float maxAllowedZoomY = boundsHeight / 2f;
            float maxAllowedZoomX = (boundsWidth / 2f) / cam.aspect;
            
            float absoluteMaxZoom = Mathf.Min(maxAllowedZoomX, maxAllowedZoomY);
            
            if (absoluteMaxZoom < minZoom) absoluteMaxZoom = minZoom;
            
            float actualMaxZoom = Mathf.Min(maxZoom, absoluteMaxZoom);
            targetZoom = Mathf.Clamp(targetZoom, minZoom, actualMaxZoom);
        }
        else
        {
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }

    private int GetActiveTouchCount()
    {
        int count = 0;
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                var phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began || 
                    phase == UnityEngine.InputSystem.TouchPhase.Moved || 
                    phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    count++;
                }
            }
        }
        return count;
    }

    void HandleZoom()
    {
        bool zoomed = false;
        
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                targetZoom -= scroll * zoomSpeedPC;
                zoomed = true;
            }
        }

        if (Touchscreen.current != null && GetActiveTouchCount() >= 2)
        {
            var touch0 = Touchscreen.current.touches[0];
            var touch1 = Touchscreen.current.touches[1];

            Vector2 touch0Pos = touch0.position.ReadValue();
            Vector2 touch1Pos = touch1.position.ReadValue();
            Vector2 touch0Delta = touch0.delta.ReadValue();
            Vector2 touch1Delta = touch1.delta.ReadValue();

            Vector2 touch0PrevPos = touch0Pos - touch0Delta;
            Vector2 touch1PrevPos = touch1Pos - touch1Delta;

            float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentMagnitude = (touch0Pos - touch1Pos).magnitude;

            targetZoom -= (currentMagnitude - prevMagnitude) * zoomSpeedMobile;
            zoomed = true;
        }

        if (zoomed)
        {
            EnforceZoomLimit();
        }
    }

    void HandlePan()
    {
        bool isPointerPressed = false;
        bool isPointerDown = false;
        bool isPointerUp = false;
        Vector2 pointerPosition = Vector2.zero;
        
        int touchCount = GetActiveTouchCount();

        if (Touchscreen.current != null && touchCount > 0)
        {
            var touch0 = Touchscreen.current.touches[0];
            var phase = touch0.phase.ReadValue();
            
            isPointerPressed = (phase == UnityEngine.InputSystem.TouchPhase.Began || phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary);
            isPointerDown = (phase == UnityEngine.InputSystem.TouchPhase.Began);
            isPointerUp = (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled);
            pointerPosition = touch0.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            isPointerPressed = Mouse.current.leftButton.isPressed;
            isPointerDown = Mouse.current.leftButton.wasPressedThisFrame;
            isPointerUp = Mouse.current.leftButton.wasReleasedThisFrame;
            pointerPosition = Mouse.current.position.ReadValue();
        }

        if (isPointerDown)
        {
            dragOrigin = pointerPosition;
            isDragging = true;
        }

        if (touchCount >= 2)
        {
            isDragging = false;
        }

        if (isDragging && isPointerPressed)
        {
            Vector2 diffScreen = pointerPosition - dragOrigin;
            
            float heightRatio = targetZoom * 2f / Screen.height;
            float widthRatio = (targetZoom * 2f * cam.aspect) / Screen.width;
            
            Vector3 difference = new Vector3(diffScreen.x * widthRatio, diffScreen.y * heightRatio, 0);
            targetPosition -= difference;
            
            dragOrigin = pointerPosition;
        }

        if (isPointerUp)
        {
            isDragging = false;
        }
    }

    private Vector3 ClampCamera(Vector3 pos, float orthoSize)
    {
        float camHeight = orthoSize;
        float camWidth = orthoSize * cam.aspect;

        float minX = minBounds.x + camWidth;
        float maxX = maxBounds.x - camWidth;
        float minY = minBounds.y + camHeight;
        float maxY = maxBounds.y - camHeight;

        if (maxX < minX)
            pos.x = (minBounds.x + maxBounds.x) / 2f;
        else
            pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if (maxY < minY)
            pos.y = (minBounds.y + maxBounds.y) / 2f;
        else
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

        return pos;
    }
} */