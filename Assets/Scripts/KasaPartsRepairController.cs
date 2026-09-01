using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class KasaPartsRepairController : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    [SerializeField] private KasaRepairPartButton[] partButtons;
    [SerializeField] private string completionSceneName = "YeniOfis";

    private bool completionRequested;
    private bool inputReady;
    private Vector3 cameraRestPosition;
    private Coroutine cameraShakeRoutine;

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (partButtons == null || partButtons.Length == 0)
        {
            partButtons = FindObjectsByType<KasaRepairPartButton>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        if (gameCamera != null)
        {
            cameraRestPosition = gameCamera.transform.position;
        }
    }

    private IEnumerator Start()
    {
        inputReady = false;

        if (partButtons == null || partButtons.Length == 0)
        {
            yield break;
        }

        foreach (KasaRepairPartButton button in partButtons)
        {
            if (button == null || button.Renderer == null)
            {
                continue;
            }

            button.Renderer.color = Color.white;
            StartCoroutine(MiniGameJuice.PopIn(
                button.transform,
                button.RestingScale,
                0.22f,
                1.2f));
            yield return new WaitForSecondsRealtime(0.045f);
        }

        yield return new WaitForSecondsRealtime(0.2f);
        inputReady = true;
    }

    private void Update()
    {
        if (MiniGamePresentationSession.IsInputBlocked)
            return;

        if (completionRequested ||
            !inputReady ||
            gameCamera == null ||
            !WasPointerPressed(out Vector2 screenPosition))
        {
            return;
        }

        float cameraDistance = Mathf.Abs(gameCamera.transform.position.z);
        Vector3 world = gameCamera.ScreenToWorldPoint(new Vector3(
            screenPosition.x,
            screenPosition.y,
            cameraDistance));

        KasaRepairPartButton selected = FindTopmostPart(world);
        if (selected == null)
            return;

        if (selected.TryBeginRepair())
        {
            StartCoroutine(AnimatePartRepair(selected));
        }
    }

    public void Configure(
        Camera configuredCamera,
        KasaRepairPartButton[] configuredButtons)
    {
        gameCamera = configuredCamera;
        partButtons = configuredButtons ?? System.Array.Empty<KasaRepairPartButton>();

        if (gameCamera != null)
        {
            cameraRestPosition = gameCamera.transform.position;
        }
    }

    private KasaRepairPartButton FindTopmostPart(Vector2 worldPosition)
    {
        KasaRepairPartButton selected = null;

        foreach (KasaRepairPartButton candidate in partButtons)
        {
            if (candidate == null || !candidate.Contains(worldPosition))
                continue;

            if (selected == null || IsDrawnAbove(candidate, selected))
                selected = candidate;
        }

        return selected;
    }

    private bool AreAllPartsHidden()
    {
        if (partButtons == null || partButtons.Length == 0)
            return false;

        foreach (KasaRepairPartButton button in partButtons)
        {
            if (button != null && button.IsVisible)
                return false;
        }

        return true;
    }

    private void CompleteTask()
    {
        if (completionRequested)
            return;

        completionRequested = true;
        inputReady = false;
        ProceduralGameAudio.Play(GameSound.TaskCompleted);
        string activeSceneName = SceneManager.GetActiveScene().name;
        TaskMissionSession.CompleteLaunchedTaskForScene(activeSceneName);
        ShakeCamera(0.055f, 0.28f);
        StartCoroutine(LoadCompletionSceneAfterFeedback());
    }

    private IEnumerator AnimatePartRepair(KasaRepairPartButton button)
    {
        ProceduralGameAudio.Play(GameSound.PartInstalled, 0.05f);
        SpriteRenderer renderer = button.Renderer;
        StartCoroutine(MiniGameJuice.FlashColor(
            renderer,
            new Color(0.4f, 0.9f, 1f),
            0.26f,
            2));
        ShakeCamera(0.025f, 0.14f);

        yield return MiniGameJuice.SquashSpinFadeOut(
            renderer,
            renderer.transform.localScale,
            0.3f,
            Random.value < 0.5f ? -65f : 65f);

        button.Hide();
        if (AreAllPartsHidden())
        {
            CompleteTask();
        }
    }

    private IEnumerator LoadCompletionSceneAfterFeedback()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        if (!string.IsNullOrWhiteSpace(completionSceneName))
        {
            SceneManager.LoadScene(completionSceneName);
        }
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
    }

    private bool IsDrawnAbove(
        KasaRepairPartButton candidate,
        KasaRepairPartButton current)
    {
        SpriteRenderer candidateRenderer = candidate.Renderer;
        SpriteRenderer currentRenderer = current.Renderer;

        int candidateLayer = SortingLayer.GetLayerValueFromID(
            candidateRenderer.sortingLayerID);
        int currentLayer = SortingLayer.GetLayerValueFromID(
            currentRenderer.sortingLayerID);

        if (candidateLayer != currentLayer)
            return candidateLayer > currentLayer;

        if (candidateRenderer.sortingOrder != currentRenderer.sortingOrder)
        {
            return candidateRenderer.sortingOrder >
                currentRenderer.sortingOrder;
        }

        float candidateDistance = Vector3.Distance(
            gameCamera.transform.position,
            candidateRenderer.bounds.center);
        float currentDistance = Vector3.Distance(
            gameCamera.transform.position,
            currentRenderer.bounds.center);

        if (!Mathf.Approximately(candidateDistance, currentDistance))
            return candidateDistance < currentDistance;

        float candidateArea = candidateRenderer.bounds.size.x *
            candidateRenderer.bounds.size.y;
        float currentArea = currentRenderer.bounds.size.x *
            currentRenderer.bounds.size.y;
        return candidateArea < currentArea;
    }

    private static bool WasPointerPressed(out Vector2 position)
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
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
