using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class VirusHuntController : MonoBehaviour
{
    [SerializeField] private Sprite[] virusSprites;
    [SerializeField] private Vector3[] virusScales;
    [SerializeField] private int totalViruses = 10;
    [SerializeField] private int maximumVisibleViruses = 5;
    [SerializeField] private float gameDuration = 12f;
    [SerializeField] private string completionSceneName = "YeniOfis";

    private readonly List<VirusView> activeViruses = new();

    private Camera gameCamera;
    private Coroutine cameraShakeRoutine;
    private Vector3 cameraRestingPosition;
    private GUIStyle counterStyle;
    private float elapsed;
    private float nextSpawnTime;
    private int spawnedViruses;
    private int destroyedViruses;
    private bool completionRequested;
    private bool timeExpired;
    private string cachedCounterText = string.Empty;
    private int cachedRemainingTenths = -1;
    private int cachedDestroyedViruses = -1;
    private bool cachedTimeExpired;

    private void Start()
    {
        gameCamera = Camera.main;
        if (gameCamera != null)
        {
            cameraRestingPosition = gameCamera.transform.position;
        }

        elapsed = 0f;
        nextSpawnTime = 0f;
        spawnedViruses = 0;
        destroyedViruses = 0;
        completionRequested = false;
        timeExpired = false;
        RefreshCounterText();

        SpawnAvailableViruses();
    }

    private void Update()
    {
        if (MiniGamePresentationSession.IsInputBlocked)
            return;

        if (completionRequested || timeExpired)
        {
            return;
        }

        elapsed = Mathf.Min(elapsed + Time.deltaTime, gameDuration);

        if (WasPointerPressed(out Vector2 screenPosition))
        {
            TryDestroyVirus(screenPosition);
        }

        SpawnAvailableViruses();

        if (destroyedViruses >= totalViruses)
        {
            RequestCompletion();
            return;
        }

        if (elapsed >= gameDuration)
        {
            timeExpired = true;
        }
    }

    public void Configure(
        Sprite[] newVirusSprites,
        Vector3[] newVirusScales)
    {
        virusSprites = newVirusSprites;
        virusScales = newVirusScales;
    }

    private void SpawnAvailableViruses()
    {
        if (virusSprites == null ||
            virusSprites.Length == 0 ||
            gameCamera == null)
        {
            return;
        }

        float spawnInterval =
            gameDuration / Mathf.Max(1, totalViruses);

        while (spawnedViruses < totalViruses &&
               activeViruses.Count < maximumVisibleViruses &&
               elapsed >= nextSpawnTime)
        {
            SpawnVirus();
            spawnedViruses++;
            nextSpawnTime += spawnInterval;
        }
    }

    private void SpawnVirus()
    {
        int spriteIndex = Random.Range(0, virusSprites.Length);

        GameObject virusObject = new(
            $"Virus_{spawnedViruses + 1:00}");

        SpriteRenderer renderer =
            virusObject.AddComponent<SpriteRenderer>();

        renderer.sprite = virusSprites[spriteIndex];
        renderer.sortingOrder = 10;

        Vector3 baseScale =
            virusScales != null &&
            spriteIndex < virusScales.Length
                ? virusScales[spriteIndex]
                : Vector3.one;

        Vector3 restingScale =
            baseScale * Random.Range(0.9f, 1.1f);
        virusObject.transform.localScale = restingScale;

        renderer.enabled = false;
        virusObject.transform.position =
            FindSpawnPosition(renderer);
        renderer.enabled = true;

        VirusView virus = new()
        {
            Renderer = renderer,
            RestingPosition = virusObject.transform.position,
            RestingScale = restingScale,
            Phase = Random.Range(0f, Mathf.PI * 2f)
        };

        activeViruses.Add(virus);
        virus.PresentationRoutine =
            StartCoroutine(AnimateVirusSpawn(virus));
    }

    private Vector3 FindSpawnPosition(SpriteRenderer renderer)
    {
        float halfHeight = gameCamera.orthographicSize;
        float halfWidth = halfHeight * gameCamera.aspect;
        Vector3 cameraPosition = gameCamera.transform.position;

        Vector2 spriteExtents = new(
            renderer.sprite.bounds.extents.x *
            Mathf.Abs(renderer.transform.lossyScale.x),
            renderer.sprite.bounds.extents.y *
            Mathf.Abs(renderer.transform.lossyScale.y));

        float minimumX =
            cameraPosition.x - halfWidth + spriteExtents.x + 0.25f;
        float maximumX =
            cameraPosition.x + halfWidth - spriteExtents.x - 0.25f;
        float minimumY =
            cameraPosition.y - halfHeight + spriteExtents.y + 0.3f;
        float maximumY =
            cameraPosition.y + halfHeight - spriteExtents.y - 1.0f;

        Vector3 candidate = Vector3.zero;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            candidate = new Vector3(
                Random.Range(minimumX, maximumX),
                Random.Range(minimumY, maximumY),
                0f);

            Bounds candidateBounds = new(
                candidate,
                new Vector3(
                    spriteExtents.x * 2f + 0.2f,
                    spriteExtents.y * 2f + 0.2f,
                    1f));

            bool overlaps = false;
            foreach (VirusView activeVirus in activeViruses)
            {
                if (activeVirus?.Renderer != null &&
                    candidateBounds.Intersects(activeVirus.Renderer.bounds))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                break;
            }
        }

        return candidate;
    }

    private void TryDestroyVirus(Vector2 screenPosition)
    {
        float cameraDistance = Mathf.Abs(
            gameCamera.transform.position.z);

        Vector3 worldPosition = gameCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                cameraDistance));

        for (int index = activeViruses.Count - 1;
             index >= 0;
             index--)
        {
            VirusView virus = activeViruses[index];
            if (virus?.Renderer == null)
            {
                activeViruses.RemoveAt(index);
                continue;
            }

            if (virus.IsDying)
            {
                continue;
            }

            Bounds bounds = virus.Renderer.bounds;
            if (!bounds.Contains(
                    new Vector3(
                        worldPosition.x,
                        worldPosition.y,
                        bounds.center.z)))
            {
                continue;
            }

            virus.IsDying = true;
            if (virus.PresentationRoutine != null)
            {
                StopCoroutine(virus.PresentationRoutine);
            }

            destroyedViruses++;
            ProceduralGameAudio.Play(GameSound.VirusDestroyed, 0.06f);
            StartCoroutine(AnimateVirusDestroyed(virus));

            if (destroyedViruses >= totalViruses)
            {
                RequestCompletion();
            }

            break;
        }
    }

    private IEnumerator AnimateVirusSpawn(VirusView virus)
    {
        yield return MiniGameJuice.PopIn(
            virus.Renderer.transform,
            virus.RestingScale,
            0.22f,
            1.2f);

        if (virus.Renderer == null || virus.IsDying)
        {
            yield break;
        }

        int difficulty = GameProgressionSession.DifficultyLevel;
        float floatHeight = Mathf.Min(
            0.11f,
            0.055f + difficulty * 0.007f);
        float minimumSpeed = 2.1f + difficulty * 0.12f;
        float maximumSpeed = 2.8f + difficulty * 0.16f;
        virus.PresentationRoutine = StartCoroutine(
            MiniGameJuice.IdleFloat(
                virus.Renderer.transform,
                virus.RestingPosition,
                virus.RestingScale,
                virus.Phase,
                floatHeight,
                Random.Range(minimumSpeed, maximumSpeed),
                Mathf.Min(6f, 3f + difficulty * 0.4f)));
    }

    private IEnumerator AnimateVirusDestroyed(VirusView virus)
    {
        if (virus.Renderer == null)
        {
            activeViruses.Remove(virus);
            yield break;
        }

        ShakeCamera();
        Vector3 currentScale = virus.Renderer.transform.localScale;
        yield return MiniGameJuice.SquashSpinFadeOut(
            virus.Renderer,
            currentScale,
            0.28f,
            Random.value < 0.5f ? -90f : 90f);

        activeViruses.Remove(virus);
        if (virus.Renderer != null)
        {
            Destroy(virus.Renderer.gameObject);
        }

        if (!completionRequested && !timeExpired)
        {
            SpawnAvailableViruses();
        }
    }

    private void ShakeCamera()
    {
        if (gameCamera == null)
        {
            return;
        }

        if (cameraShakeRoutine != null)
        {
            StopCoroutine(cameraShakeRoutine);
            gameCamera.transform.position = cameraRestingPosition;
        }

        cameraShakeRoutine = StartCoroutine(
            MiniGameJuice.ShakePosition(
                gameCamera.transform,
                cameraRestingPosition,
                0.035f,
                0.16f,
                55f));
    }

    private void RequestCompletion()
    {
        if (completionRequested)
        {
            return;
        }

        completionRequested = true;
        ProceduralGameAudio.Play(GameSound.TaskCompleted);
        TaskMissionSession.CompleteLaunchedTaskForScene(
            SceneManager.GetActiveScene().name);
        StartCoroutine(CompleteAfterFeedback());
    }

    private IEnumerator CompleteAfterFeedback()
    {
        yield return new WaitForSecondsRealtime(0.42f);

        if (string.IsNullOrWhiteSpace(completionSceneName))
        {
            Debug.LogError(
                "Virus Hunt completion scene is missing.",
                this);
            yield break;
        }

        SceneManager.LoadScene(completionSceneName);
    }

    private void OnDisable()
    {
        if (gameCamera != null)
        {
            gameCamera.transform.position = cameraRestingPosition;
        }
    }

    private void OnGUI()
    {
        if (counterStyle == null)
        {
            counterStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(
                    22,
                    Mathf.RoundToInt(Screen.height * 0.025f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        RefreshCounterText();

        float counterTop =
            Mathf.Max(70f, Screen.height * 0.05f);
        float counterHeight =
            Mathf.Max(60f, Screen.height * 0.04f);

        GUI.Label(
            new Rect(
                0f,
                counterTop,
                Screen.width,
                counterHeight),
            cachedCounterText,
            counterStyle);
    }

    private void RefreshCounterText()
    {
        float remainingTime = Mathf.Max(0f, gameDuration - elapsed);
        int remainingTenths = Mathf.CeilToInt(remainingTime * 10f);
        if (cachedRemainingTenths == remainingTenths &&
            cachedDestroyedViruses == destroyedViruses &&
            cachedTimeExpired == timeExpired)
        {
            return;
        }

        cachedRemainingTenths = remainingTenths;
        cachedDestroyedViruses = destroyedViruses;
        cachedTimeExpired = timeExpired;
        cachedCounterText = timeExpired
            ? $"Time's up!   Viruses: {destroyedViruses}/{totalViruses}"
            : $"Time: {remainingTenths / 10f:0.0}   Viruses: {destroyedViruses}/{totalViruses}";
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

    private sealed class VirusView
    {
        public SpriteRenderer Renderer;
        public Coroutine PresentationRoutine;
        public Vector3 RestingPosition;
        public Vector3 RestingScale;
        public float Phase;
        public bool IsDying;
    }
}
