using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class NoTaskRoomFeedback : MonoBehaviour
{
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Image feedbackImage;
    [SerializeField] private Sprite[] feedbackSprites;
    [SerializeField, Min(0.1f)] private float displayDuration = 2f;
    [SerializeField, Min(0f)] private float initialInputGuardDuration = 0.2f;
    [SerializeField, Min(0f)] private float dismissInputGuardDuration = 0.75f;

    private float remainingTime;
    private float dismissShieldTime;
    private float acceptRequestsAt;
    private CanvasGroup overlayCanvasGroup;
    private bool dismissing;
    private bool selectionInputArmed;
    private int fullyReleasedFrame = -1;
    private int shownFrame = -1;
    private int lastSpriteIndex = -1;

    public bool IsShowing =>
        overlayRoot != null && overlayRoot.activeSelf;
    public bool CanAcceptRoomSelection =>
        selectionInputArmed &&
        Time.unscaledTime >= acceptRequestsAt &&
        !IsShowing;

    private void Awake()
    {
        if (overlayRoot != null)
        {
            overlayCanvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            if (overlayCanvasGroup == null)
                overlayCanvasGroup = overlayRoot.AddComponent<CanvasGroup>();
        }

        acceptRequestsAt =
            Time.unscaledTime + initialInputGuardDuration;
        selectionInputArmed = false;
        fullyReleasedFrame = -1;
        HideFeedback();
    }

    private void Update()
    {
        UpdateSelectionInputGate();

        if (!IsShowing)
            return;

        if (dismissing)
        {
            dismissShieldTime -= Time.unscaledDeltaTime;
            if (dismissShieldTime <= 0f)
                HideFeedback();
            return;
        }

        // Do not let the same press that opened the overlay close it again.
        if (Time.frameCount > shownFrame && WasPointerPressedThisFrame())
        {
            // Make the feedback invisible immediately, but keep its raycast
            // shield alive until this press/release has completely finished.
            acceptRequestsAt =
                Time.unscaledTime + dismissInputGuardDuration;
            dismissing = true;
            dismissShieldTime = dismissInputGuardDuration;
            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = 0f;
            else
                feedbackImage.enabled = false;
            return;
        }

        remainingTime -= Time.unscaledDeltaTime;
        if (remainingTime <= 0f)
            HideFeedback();
    }

    public void ShowRandomFeedback()
    {
        // Scene-changing presses can remain active during the first frames of
        // the newly loaded floor scene. Ignore only that short carry-over.
        if (!CanAcceptRoomSelection ||
            overlayRoot == null ||
            feedbackImage == null ||
            feedbackSprites == null ||
            feedbackSprites.Length == 0)
        {
            return;
        }

        int selectedIndex = SelectSpriteIndex();
        feedbackImage.sprite = feedbackSprites[selectedIndex];
        feedbackImage.enabled = feedbackImage.sprite != null;
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 1f;
            overlayCanvasGroup.blocksRaycasts = true;
            overlayCanvasGroup.interactable = true;
        }
        overlayRoot.SetActive(true);
        remainingTime = displayDuration;
        dismissing = false;
        dismissShieldTime = 0f;
        shownFrame = Time.frameCount;
        lastSpriteIndex = selectedIndex;
    }

    private void UpdateSelectionInputGate()
    {
        if (selectionInputArmed)
            return;

        if (IsPointerHeld())
        {
            fullyReleasedFrame = -1;
            return;
        }

        if (fullyReleasedFrame < 0)
        {
            fullyReleasedFrame = Time.frameCount;
            return;
        }

        if (Time.frameCount > fullyReleasedFrame)
            selectionInputArmed = true;
    }

    private int SelectSpriteIndex()
    {
        if (feedbackSprites.Length == 1)
            return 0;

        int selectedIndex;
        do
        {
            selectedIndex =
                Random.Range(0, feedbackSprites.Length);
        }
        while (selectedIndex == lastSpriteIndex);

        return selectedIndex;
    }

    private void HideFeedback()
    {
        remainingTime = 0f;
        dismissShieldTime = 0f;
        dismissing = false;
        shownFrame = -1;
        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        if (overlayCanvasGroup != null)
            overlayCanvasGroup.alpha = 1f;
    }

    private static bool WasPointerPressedThisFrame()
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
    }

    private static bool IsPointerHeld()
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }

        return Mouse.current != null &&
               Mouse.current.leftButton.isPressed;
    }

#if UNITY_EDITOR
    public void ConfigureEditor(
        GameObject configuredOverlayRoot,
        Image configuredFeedbackImage,
        Sprite[] configuredSprites)
    {
        overlayRoot = configuredOverlayRoot;
        feedbackImage = configuredFeedbackImage;
        feedbackSprites = configuredSprites;
        displayDuration = 2f;
        initialInputGuardDuration = 0.2f;
        dismissInputGuardDuration = 0.75f;
    }
#endif
}
