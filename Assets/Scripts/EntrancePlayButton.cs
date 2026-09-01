using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class EntrancePlayButton : MonoBehaviour
{
    [SerializeField] private string targetScene = "YeniOfis";
    [SerializeField, Min(0f)] private float bobHeight = 0.12f;
    [SerializeField, Min(0.1f)] private float bobSpeed = 2.2f;

    private Camera gameCamera;
    private SpriteRenderer buttonRenderer;
    private Vector3 restingPosition;
    private bool sceneLoadRequested;

    private void Awake()
    {
        gameCamera = Camera.main;
        buttonRenderer = GetComponent<SpriteRenderer>();
        restingPosition = transform.position;
    }

    private void Update()
    {
        float offset =
            Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight;
        transform.position =
            restingPosition + (Vector3.up * offset);

        if (sceneLoadRequested ||
            !WasPointerPressed(out Vector2 screenPosition) ||
            !IsPointerOverButton(screenPosition))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError(
                "Entrance play button target scene is missing.",
                this);
            return;
        }

        sceneLoadRequested = true;
        ProceduralGameAudio.Play(GameSound.UiClick, 0.025f);
        BossIntroDialogue.BeginNewGame();
        SceneManager.LoadScene(targetScene);
    }

    private bool IsPointerOverButton(Vector2 screenPosition)
    {
        if (gameCamera == null ||
            buttonRenderer == null ||
            !buttonRenderer.enabled)
        {
            return false;
        }

        float cameraDistance = Mathf.Abs(
            gameCamera.transform.position.z -
            transform.position.z);
        Vector3 worldPosition = gameCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                cameraDistance));
        Bounds bounds = buttonRenderer.bounds;

        return bounds.Contains(
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                bounds.center.z));
    }

    private static bool WasPointerPressed(out Vector2 position)
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            position =
                Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        position = Vector2.zero;
        return false;
    }
}
