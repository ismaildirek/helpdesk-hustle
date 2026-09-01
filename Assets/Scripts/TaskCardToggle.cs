using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class TaskCardToggle : MonoBehaviour
{
    [SerializeField] private GameObject taskCard = null;
    [FormerlySerializedAs("taskCardBoss")]
    [SerializeField] private GameObject taskCardDecoration = null;
    [SerializeField] private BossAngerMeter bossAngerMeter;

    [Header("Notification Animation")]
    [SerializeField, Min(0.5f)] private float notificationInterval = 3.2f;
    [SerializeField, Range(0f, 0.2f)] private float notificationScale = 0.08f;
    [SerializeField] private Color notificationTint =
        new(0.35f, 0.9f, 1f, 1f);

    private Camera gameCamera;
    private SpriteRenderer trophyRenderer;
    private SpriteRenderer taskCardRenderer;
    private Vector3 trophyRestScale;
    private Color trophyRestColor;
    private Vector3 taskCardRestScale;
    private Vector3 decorationRestScale;
    private Coroutine cardAnimationRoutine;
    private Coroutine completionFeedbackRoutine;
    private bool isTaskCardVisible;

    private void Awake()
    {
        gameCamera = Camera.main;
        trophyRenderer = GetComponent<SpriteRenderer>();
        taskCardRenderer = taskCard != null
            ? taskCard.GetComponent<SpriteRenderer>()
            : null;
        if (bossAngerMeter == null)
        {
            bossAngerMeter = FindFirstObjectByType<BossAngerMeter>(
                FindObjectsInactive.Include);
        }
        trophyRestScale = transform.localScale;
        trophyRestColor = trophyRenderer != null
            ? trophyRenderer.color
            : Color.white;
        taskCardRestScale = taskCard != null
            ? taskCard.transform.localScale
            : Vector3.one;
        decorationRestScale = taskCardDecoration != null
            ? taskCardDecoration.transform.localScale
            : Vector3.one;
        SetTaskCardVisible(false);
    }

    private void Update()
    {
        AnimateNotification();

        if (BossIntroDialogue.IsBlockingOfficeInput ||
            GamePauseSession.IsPaused ||
            !WasPointerPressed(out Vector2 screenPosition) ||
            !IsPointerOverTrophy(screenPosition))
        {
            return;
        }

        ProceduralGameAudio.Play(GameSound.UiClick, 0.025f);
        if (completionFeedbackRoutine != null)
        {
            StopCoroutine(completionFeedbackRoutine);
            completionFeedbackRoutine = null;
        }
        SetTaskCardVisible(!isTaskCardVisible);
    }

    public void ShowCompletionFeedback(float visibleDuration)
    {
        if (completionFeedbackRoutine != null)
            StopCoroutine(completionFeedbackRoutine);

        SetTaskCardVisible(true);
        completionFeedbackRoutine = StartCoroutine(
            HideAfterCompletionFeedback(visibleDuration));
    }

    private System.Collections.IEnumerator HideAfterCompletionFeedback(
        float visibleDuration)
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.5f, visibleDuration));

        if (isTaskCardVisible)
            SetTaskCardVisible(false);

        completionFeedbackRoutine = null;
    }

    private void SetTaskCardVisible(bool isVisible)
    {
        isTaskCardVisible = isVisible;

        if (cardAnimationRoutine != null)
        {
            StopCoroutine(cardAnimationRoutine);
            cardAnimationRoutine = null;
        }

        RestoreCardScales();
        SetActiveIfAssigned(taskCard, isVisible);
        SetActiveIfAssigned(taskCardDecoration, isVisible);
        if (bossAngerMeter != null)
        {
            int cardSortingOrder = taskCardRenderer != null
                ? taskCardRenderer.sortingOrder
                : 5;
            bossAngerMeter.SetCoveredByTaskCard(
                isVisible,
                cardSortingOrder);
        }

        if (isVisible)
        {
            transform.localScale = trophyRestScale;
            if (trophyRenderer != null)
            {
                trophyRenderer.color = trophyRestColor;
            }

            cardAnimationRoutine = StartCoroutine(AnimateCardOpen());
        }
    }

    private void AnimateNotification()
    {
        if (isTaskCardVisible ||
            BossIntroDialogue.IsBlockingOfficeInput ||
            BossAngerSession.HasLost)
        {
            transform.localScale = trophyRestScale;
            if (trophyRenderer != null)
            {
                trophyRenderer.color = trophyRestColor;
            }
            return;
        }

        float cycle = Mathf.Repeat(
            Time.unscaledTime,
            Mathf.Max(0.5f, notificationInterval));
        float normalized = cycle / Mathf.Max(0.5f, notificationInterval);
        float wave = normalized < 0.24f
            ? Mathf.Sin(normalized / 0.24f * Mathf.PI)
            : 0f;
        wave *= wave;

        transform.localScale = trophyRestScale *
            (1f + wave * notificationScale);

        if (trophyRenderer != null)
        {
            trophyRenderer.color = Color.Lerp(
                trophyRestColor,
                notificationTint,
                wave * 0.55f);
        }
    }

    private System.Collections.IEnumerator AnimateCardOpen()
    {
        if (taskCard != null)
        {
            StartCoroutine(MiniGameJuice.PopIn(
                taskCard.transform,
                taskCardRestScale,
                0.24f,
                1.08f));
        }

        if (taskCardDecoration != null)
        {
            yield return new WaitForSecondsRealtime(0.05f);
            yield return MiniGameJuice.PopIn(
                taskCardDecoration.transform,
                decorationRestScale,
                0.22f,
                1.1f);
        }

        cardAnimationRoutine = null;
    }

    private void RestoreCardScales()
    {
        if (taskCard != null)
        {
            taskCard.transform.localScale = taskCardRestScale;
        }

        if (taskCardDecoration != null)
        {
            taskCardDecoration.transform.localScale = decorationRestScale;
        }
    }

    private void OnDisable()
    {
        completionFeedbackRoutine = null;
        if (bossAngerMeter != null)
            bossAngerMeter.SetCoveredByTaskCard(false, 5);
        transform.localScale = trophyRestScale;
        if (trophyRenderer != null)
        {
            trophyRenderer.color = trophyRestColor;
        }

        RestoreCardScales();
    }

    private bool IsPointerOverTrophy(Vector2 screenPosition)
    {
        if (gameCamera == null ||
            trophyRenderer == null ||
            !trophyRenderer.enabled)
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

        Bounds trophyBounds = trophyRenderer.bounds;
        return trophyBounds.Contains(
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                trophyBounds.center.z));
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

    private static void SetActiveIfAssigned(
        GameObject target,
        bool isActive)
    {
        if (target != null && target.activeSelf != isActive)
        {
            target.SetActive(isActive);
        }
    }
}
