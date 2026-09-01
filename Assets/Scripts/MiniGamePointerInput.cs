using UnityEngine;
using UnityEngine.InputSystem;

internal static class MiniGamePointerInput
{
    public static bool WasPressed(out Vector2 screenPosition)
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition =
                Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = Vector2.zero;
        return false;
    }

    public static bool TryGetWorldPosition(
        Camera gameCamera,
        Vector2 screenPosition,
        out Vector2 worldPosition)
    {
        worldPosition = default;
        if (gameCamera == null)
            return false;

        float distance = Mathf.Abs(gameCamera.transform.position.z);
        Vector3 world = gameCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distance));
        worldPosition = new Vector2(world.x, world.y);
        return true;
    }

    public static bool IsNear(
        SpriteRenderer renderer,
        Vector2 worldPosition,
        float radius)
    {
        if (renderer == null || !renderer.enabled ||
            !renderer.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector2 center = renderer.transform.position;
        return Vector2.SqrMagnitude(worldPosition - center) <=
               radius * radius;
    }
}
