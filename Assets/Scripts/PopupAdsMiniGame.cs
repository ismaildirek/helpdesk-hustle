using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PopupAdsMiniGame : MonoBehaviour
{
    private enum PopupKind
    {
        Good,
        Bad,
        Neutral
    }

    private enum PopupAction
    {
        Minimize,
        Close
    }

    private readonly struct PopupEntry
    {
        public PopupEntry(Sprite sprite, PopupKind kind)
        {
            Sprite = sprite;
            Kind = kind;
        }

        public Sprite Sprite { get; }
        public PopupKind Kind { get; }
    }

    [Header("Scene References")]
    [SerializeField] private SpriteRenderer backgroundRenderer = null;
    [SerializeField] private SpriteRenderer popupRenderer = null;
    [SerializeField] private SpriteRenderer alertIcon = null;

    [Header("Popup Assets")]
    [SerializeField] private Sprite[] goodAds = null;
    [SerializeField] private Sprite[] badAds = null;
    [SerializeField] private Sprite[] neutralAds = null;

    [Header("Layout")]
    [SerializeField, Range(0.3f, 0.9f)]
    private float maximumPopupWidthFactor = 0.72f;
    [SerializeField, Range(0.2f, 0.8f)]
    private float maximumPopupHeightFactor = 0.44f;
    [SerializeField, Range(0.5f, 2f)]
    private float alertRenderedHeight = 1.1f;

    [Header("Rules")]
    [SerializeField] private string completionSceneName = "YeniOfis";

    private readonly List<PopupEntry> popupSequence = new(16);
    private Camera gameCamera;
    private int currentPopupIndex;
    private bool alertIsVisible;
    private bool completionRequested;
    private bool inputLocked;
    private Vector3 popupRestingPosition;
    private Vector3 popupRestingScale;

    private void Awake()
    {
        gameCamera = Camera.main;

        if (!HasRequiredReferences())
        {
            Debug.LogError(
                "Popup Ads mini game is missing its camera, renderers or popup assets.",
                this);
            enabled = false;
            return;
        }

        StartGame();
    }

    private void Update()
    {
        if (MiniGamePresentationSession.IsInputBlocked)
            return;

        if (completionRequested ||
            !WasPointerPressed(out Vector2 screenPosition) ||
            !TryGetPointerWorldPosition(screenPosition, out Vector2 worldPosition))
        {
            return;
        }

        if (alertIsVisible)
        {
            if (!inputLocked &&
                ContainsPoint(alertIcon, worldPosition))
            {
                HideAlert();
            }

            return;
        }

        if (inputLocked)
        {
            return;
        }

        if (TryGetPopupAction(worldPosition, out PopupAction action))
        {
            SubmitAction(action);
        }
    }

    private void StartGame()
    {
        popupSequence.Clear();
        AddEntries(goodAds, PopupKind.Good);
        AddEntries(badAds, PopupKind.Bad);
        AddEntries(neutralAds, PopupKind.Neutral);
        Shuffle(popupSequence);

        currentPopupIndex = 0;
        completionRequested = false;
        inputLocked = false;
        popupRenderer.sortingOrder = backgroundRenderer.sortingOrder + 20;
        alertIcon.sortingOrder = popupRenderer.sortingOrder + 20;
        HideAlert();
        ShowCurrentPopup();
    }

    private void SubmitAction(PopupAction action)
    {
        PopupKind currentKind = popupSequence[currentPopupIndex].Kind;
        bool isCorrect = currentKind == PopupKind.Neutral ||
            (currentKind == PopupKind.Good && action == PopupAction.Minimize) ||
            (currentKind == PopupKind.Bad && action == PopupAction.Close);

        if (!isCorrect)
        {
            ShowAlert();
            return;
        }

        StartCoroutine(AnimateAcceptedAction(action));
    }

    private void ShowCurrentPopup()
    {
        popupRenderer.enabled = true;
        popupRenderer.sprite = popupSequence[currentPopupIndex].Sprite;
        popupRenderer.color = Color.white;
        popupRenderer.transform.localScale = Vector3.one;
        FitAndPlacePopup();
        popupRestingPosition = popupRenderer.transform.position;
        popupRestingScale = popupRenderer.transform.localScale;
        StartCoroutine(AnimatePopupEntrance());
    }

    private IEnumerator AnimatePopupEntrance()
    {
        inputLocked = true;
        yield return MiniGameJuice.PopIn(
            popupRenderer.transform,
            popupRestingScale,
            0.22f,
            1.2f);
        inputLocked = false;
    }

    private IEnumerator AnimateAcceptedAction(PopupAction action)
    {
        inputLocked = true;
        ProceduralGameAudio.Play(
            GameSound.PopupAction,
            action == PopupAction.Minimize ? 0.035f : 0f);

        Vector3 destination = popupRestingPosition;
        Vector3 finalScale = popupRestingScale * 0.08f;

        if (action == PopupAction.Minimize)
        {
            Bounds backgroundBounds = backgroundRenderer.bounds;
            destination = new Vector3(
                backgroundBounds.center.x,
                backgroundBounds.min.y + backgroundBounds.size.y * 0.075f,
                popupRestingPosition.z);
            finalScale = popupRestingScale * 0.14f;
        }

        yield return MiniGameJuice.MoveScaleFade(
            popupRenderer,
            popupRestingPosition,
            destination,
            popupRestingScale,
            finalScale,
            action == PopupAction.Minimize ? 0.28f : 0.2f);

        popupRenderer.enabled = false;
        currentPopupIndex++;
        if (currentPopupIndex >= popupSequence.Count)
        {
            CompleteGame();
            yield break;
        }

        ShowCurrentPopup();
    }

    private void FitAndPlacePopup()
    {
        Bounds backgroundBounds = backgroundRenderer.bounds;
        Vector2 spriteSize = popupRenderer.sprite.bounds.size;

        float maximumWidth = backgroundBounds.size.x * maximumPopupWidthFactor;
        float maximumHeight = backgroundBounds.size.y * maximumPopupHeightFactor;
        float scale = Mathf.Min(
            maximumWidth / Mathf.Max(0.001f, spriteSize.x),
            maximumHeight / Mathf.Max(0.001f, spriteSize.y));

        popupRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        Bounds popupBounds = popupRenderer.bounds;

        float horizontalPadding = backgroundBounds.size.x * 0.035f;
        float minimumX = Mathf.Max(
            backgroundBounds.min.x + popupBounds.extents.x + horizontalPadding,
            Mathf.Lerp(backgroundBounds.min.x, backgroundBounds.max.x, 0.38f));
        float maximumX = backgroundBounds.max.x -
            popupBounds.extents.x - horizontalPadding;

        float minimumY = backgroundBounds.min.y +
            backgroundBounds.size.y * 0.2f + popupBounds.extents.y;
        float maximumY = backgroundBounds.max.y -
            backgroundBounds.size.y * 0.11f - popupBounds.extents.y;

        float x = minimumX <= maximumX
            ? Random.Range(minimumX, maximumX)
            : backgroundBounds.center.x;
        float y = minimumY <= maximumY
            ? Random.Range(minimumY, maximumY)
            : backgroundBounds.center.y;

        popupRenderer.transform.position = new Vector3(x, y, -1f);
    }

    private bool TryGetPopupAction(
        Vector2 pointerWorld,
        out PopupAction action)
    {
        action = default;

        if (!popupRenderer.enabled ||
            !popupRenderer.bounds.Contains(new Vector3(
                pointerWorld.x,
                pointerWorld.y,
                popupRenderer.bounds.center.z)))
        {
            return false;
        }

        Bounds bounds = popupRenderer.bounds;
        float normalizedX = Mathf.InverseLerp(
            bounds.min.x,
            bounds.max.x,
            pointerWorld.x);
        float normalizedY = Mathf.InverseLerp(
            bounds.min.y,
            bounds.max.y,
            pointerWorld.y);

        if (normalizedY < 0.8f)
        {
            return false;
        }

        if (normalizedX >= 0.83f)
        {
            action = PopupAction.Close;
            return true;
        }

        if (normalizedX >= 0.66f && normalizedX <= 0.81f)
        {
            action = PopupAction.Minimize;
            return true;
        }

        return false;
    }

    private void ShowAlert()
    {
        MiniGamePerformanceSession.RegisterMistake();
        ProceduralGameAudio.Play(GameSound.WrongAction, 0.025f);
        SetRenderedHeight(alertIcon, alertRenderedHeight);
        alertIcon.enabled = true;

        Vector3 targetCenter = popupRenderer.bounds.center;
        targetCenter.z = -2f;
        alertIcon.transform.position = targetCenter;

        // Some pixel sprites use a non-centered pivot. Correct against the
        // rendered bounds so the warning always appears over the popup.
        Vector3 centerCorrection = targetCenter - alertIcon.bounds.center;
        alertIcon.transform.position += centerCorrection;
        alertIsVisible = true;
        StartCoroutine(AnimateWrongAction());
    }

    private IEnumerator AnimateWrongAction()
    {
        inputLocked = true;
        Vector3 alertScale = alertIcon.transform.localScale;
        StartCoroutine(MiniGameJuice.ShakePosition(
            popupRenderer.transform,
            popupRestingPosition,
            0.075f,
            0.24f,
            52f));
        yield return MiniGameJuice.PopIn(
            alertIcon.transform,
            alertScale,
            0.2f,
            1.25f);
        inputLocked = false;
    }

    private void HideAlert()
    {
        alertIsVisible = false;
        alertIcon.enabled = false;
        alertIcon.transform.localScale = Vector3.one;
    }

    private void CompleteGame()
    {
        if (completionRequested)
        {
            return;
        }

        completionRequested = true;
        inputLocked = true;
        ProceduralGameAudio.Play(GameSound.TaskCompleted);
        popupRenderer.enabled = false;
        HideAlert();

        string activeSceneName = SceneManager.GetActiveScene().name;
        TaskMissionSession.CompleteLaunchedTaskForScene(activeSceneName);

        if (string.IsNullOrWhiteSpace(completionSceneName))
        {
            Debug.LogError(
                "Popup Ads mini game completion scene is missing.",
                this);
            return;
        }

        SceneManager.LoadScene(completionSceneName);
    }

    private void AddEntries(Sprite[] sprites, PopupKind kind)
    {
        foreach (Sprite sprite in sprites)
        {
            if (sprite != null)
            {
                popupSequence.Add(new PopupEntry(sprite, kind));
            }
        }
    }

    private bool HasRequiredReferences()
    {
        return gameCamera != null &&
            backgroundRenderer != null &&
            popupRenderer != null &&
            alertIcon != null &&
            HasEverySprite(goodAds, 6) &&
            HasEverySprite(badAds, 6) &&
            HasEverySprite(neutralAds, 4);
    }

    private static bool HasEverySprite(Sprite[] sprites, int expectedCount)
    {
        if (sprites == null || sprites.Length != expectedCount)
        {
            return false;
        }

        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryGetPointerWorldPosition(
        Vector2 screenPosition,
        out Vector2 worldPosition)
    {
        float cameraDistance = Mathf.Abs(
            gameCamera.transform.position.z - popupRenderer.transform.position.z);
        Vector3 converted = gameCamera.ScreenToWorldPoint(new Vector3(
            screenPosition.x,
            screenPosition.y,
            cameraDistance));
        worldPosition = converted;
        return true;
    }

    private static bool ContainsPoint(
        SpriteRenderer renderer,
        Vector2 worldPosition)
    {
        if (renderer == null || !renderer.enabled)
        {
            return false;
        }

        Bounds bounds = renderer.bounds;
        return bounds.Contains(new Vector3(
            worldPosition.x,
            worldPosition.y,
            bounds.center.z));
    }

    private static void SetRenderedHeight(
        SpriteRenderer renderer,
        float renderedHeight)
    {
        float spriteHeight = Mathf.Max(0.001f, renderer.sprite.bounds.size.y);
        float scale = renderedHeight / spriteHeight;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static void Shuffle(List<PopupEntry> entries)
    {
        for (int index = entries.Count - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            (entries[index], entries[swapIndex]) =
                (entries[swapIndex], entries[index]);
        }
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
