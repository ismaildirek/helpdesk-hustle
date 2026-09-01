using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer))]
public class SceneIconButton : MonoBehaviour
{
    [SerializeField] private string targetScene;

    private Camera gameCamera;
    private SpriteRenderer iconRenderer;
    private bool sceneLoadRequested;

    private void Awake()
    {
        gameCamera = Camera.main;
        iconRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (BossIntroDialogue.IsBlockingOfficeInput ||
            GamePauseSession.IsPaused ||
            sceneLoadRequested ||
            !WasPointerPressed(out Vector2 screenPosition) ||
            !IsPointerOverIcon(screenPosition))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError(
                $"Target scene is missing on {gameObject.name}.",
                this);
            return;
        }

        sceneLoadRequested = true;
        ProceduralGameAudio.Play(GameSound.UiClick, 0.025f);
        TaskMissionSession.AbandonLaunchedTaskForScene(
            SceneManager.GetActiveScene().name);

        if (BossAngerSession.HasLost)
            return;

        SceneManager.LoadScene(targetScene);
    }

    public void Configure(string newTargetScene)
    {
        targetScene = newTargetScene;
    }

    private bool IsPointerOverIcon(Vector2 screenPosition)
    {
        if (gameCamera == null ||
            iconRenderer == null ||
            !iconRenderer.enabled)
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

        Bounds bounds = iconRenderer.bounds;
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
