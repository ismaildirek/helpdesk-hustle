using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Zoom")]
    public float minZoom = 2f;
    public float maxZoom = 5f;
    public float zoomSpeed = 0.5f;

    [Header("Movement")]
    public float moveSmooth = 12f;

    [Header("Bounds")]
    public bool useBounds = true;
    public Vector2 minBounds = new Vector2(-2.85f, -5.05f);
    public Vector2 maxBounds = new Vector2(10.95f, 5.00f);

    private Camera cam;

    private float targetZoom;
    private Vector3 targetPos;

    private bool dragging;
    private Vector2 lastPointer;

    void Awake()
    {
        cam = GetComponent<Camera>();

        targetZoom = cam.orthographicSize;
        targetPos = transform.position;
    }

    void LateUpdate()
    {
        ZoomInput();
        DragInput();

        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);

        cam.orthographicSize = Mathf.Lerp(
        cam.orthographicSize,
        targetZoom,
        Time.deltaTime * 8f);

        if (useBounds)
            targetPos = Clamp(targetPos);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * moveSmooth);
    }

    void ZoomInput()
    {
        // ===== PC Mouse Scroll =====
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;


            if (Mathf.Abs(scroll) > 0.01f)
            {
                ZoomToMouse(scroll * zoomSpeed);
            }
        }

        // ===== Mobil Pinch =====
        if (Touchscreen.current != null &&
            Touchscreen.current.touches.Count >= 2)
        {
            var touch0 = Touchscreen.current.touches[0];
            var touch1 = Touchscreen.current.touches[1];

            if (touch0.press.isPressed && touch1.press.isPressed)
            {
                Vector2 p0 = touch0.position.ReadValue();
                Vector2 p1 = touch1.position.ReadValue();

                Vector2 d0 = touch0.delta.ReadValue();
                Vector2 d1 = touch1.delta.ReadValue();

                float previousDistance = ((p0 - d0) - (p1 - d1)).magnitude;
                float currentDistance = (p0 - p1).magnitude;

                float delta = currentDistance - previousDistance;

                targetZoom -= delta * zoomSpeed * Time.deltaTime;
            }
        }

        // Zoom sýnýrlarý
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }



    void DragInput()
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 pos = Touchscreen.current.primaryTouch.position.ReadValue();

            if (!dragging)
            {
                dragging = true;
                lastPointer = pos;
            }

            Vector2 delta = pos - lastPointer;

            targetPos -= new Vector3(
                delta.x * cam.orthographicSize * cam.aspect / Screen.width * 2f,
                delta.y * cam.orthographicSize / Screen.height * 2f,
                0);

            lastPointer = pos;

            return;
        }

        if (Touchscreen.current != null &&
            !Touchscreen.current.primaryTouch.press.isPressed)
        {
            dragging = false;
        }

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            dragging = true;
            lastPointer = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            dragging = false;
        }

        if (dragging)
        {
            Vector2 pos = Mouse.current.position.ReadValue();

            Vector2 delta = pos - lastPointer;

            targetPos -= new Vector3(
                delta.x * cam.orthographicSize * cam.aspect / Screen.width * 2f,
                delta.y * cam.orthographicSize / Screen.height * 2f,
                0);

            lastPointer = pos;
        }
    }

    Vector3 Clamp(Vector3 pos)
    {
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float left = minBounds.x + camWidth;
        float right = maxBounds.x - camWidth;

        float bottom = minBounds.y + camHeight;
        float top = maxBounds.y - camHeight;

        if (left > right)
            pos.x = (minBounds.x + maxBounds.x) * 0.5f;
        else
            pos.x = Mathf.Clamp(pos.x, left, right);

        if (bottom > top)
            pos.y = (minBounds.y + maxBounds.y) * 0.5f;
        else
            pos.y = Mathf.Clamp(pos.y, bottom, top);

        pos.z = -10;

        return pos;
    }

    void ZoomToMouse(float zoomAmount)
    {
        Vector3 mouseWorldBefore = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldBefore.z = 0;

        targetZoom = Mathf.Clamp(targetZoom - zoomAmount, minZoom, maxZoom);

        // Yeni zoom'u geçici olarak uygula
        float oldSize = cam.orthographicSize;
        cam.orthographicSize = targetZoom;

        Vector3 mouseWorldAfter = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldAfter.z = 0;

        // Kamerayý mouse'un altýndaki dünya noktasý sabit kalacak þekilde kaydýr
        targetPos += mouseWorldBefore - mouseWorldAfter;

        // Kamerayý eski haline getir (LateUpdate Lerp ile geçecek)
        cam.orthographicSize = oldSize;
    }
}