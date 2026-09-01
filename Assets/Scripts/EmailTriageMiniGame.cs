using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EmailTriageMiniGame : MonoBehaviour
{
    private enum EmailKind
    {
        Safe,
        Malicious
    }

    [Header("Email Views")]
    [SerializeField] private SpriteRenderer safeEmail;
    [SerializeField] private SpriteRenderer maliciousEmail;
    [SerializeField] private SpriteRenderer alertIcon;

    [Header("Answer Buttons")]
    [SerializeField] private SpriteRenderer safeButton;
    [SerializeField] private SpriteRenderer maliciousButton;

    [Header("Rules")]
    [SerializeField, Min(1)] private int emailCount = 7;
    [SerializeField, Min(0f)] private float wrongAnswerLockSeconds = 0.5f;
    [SerializeField, Range(0.2f, 1f)] private float buttonHitRadiusFactor = 0.72f;
    [SerializeField] private string completionSceneName = "YeniOfis";

    private readonly List<EmailKind> emailSequence = new();
    private Camera gameCamera;
    private int currentEmailIndex;
    private bool inputLocked;
    private bool completionRequested;
    private Vector3 safeEmailRestPosition;
    private Vector3 maliciousEmailRestPosition;
    private Vector3 safeEmailRestScale;
    private Vector3 maliciousEmailRestScale;
    private Vector3 safeButtonRestScale;
    private Vector3 maliciousButtonRestScale;
    private Vector3 alertRestScale;

    private void Awake()
    {
        gameCamera = Camera.main;
        StartGame();
    }

    private void Update()
    {
        if (MiniGamePresentationSession.IsInputBlocked)
            return;

        if (inputLocked ||
            currentEmailIndex >= emailSequence.Count ||
            !WasPointerPressed(out Vector2 screenPosition))
        {
            return;
        }

        if (TryGetClickedAnswer(screenPosition, out EmailKind answer))
        {
            SubmitAnswer(answer);
        }
    }

    public void Configure(
        SpriteRenderer newSafeEmail,
        SpriteRenderer newMaliciousEmail,
        SpriteRenderer newAlertIcon,
        SpriteRenderer newSafeButton,
        SpriteRenderer newMaliciousButton,
        int newEmailCount,
        float newWrongAnswerLockSeconds)
    {
        safeEmail = newSafeEmail;
        maliciousEmail = newMaliciousEmail;
        alertIcon = newAlertIcon;
        safeButton = newSafeButton;
        maliciousButton = newMaliciousButton;
        emailCount = Mathf.Max(1, newEmailCount);
        wrongAnswerLockSeconds = Mathf.Max(0f, newWrongAnswerLockSeconds);
    }

    private void StartGame()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError(
                "E-posta mini game is missing an email, button or alert reference.",
                this);
            enabled = false;
            return;
        }

        emailSequence.Clear();
        int difficultyBonus = Mathf.Min(
            3,
            GameProgressionSession.DifficultyLevel / 2);
        BuildBalancedRandomSequence(
            emailSequence,
            emailCount + difficultyBonus);
        CacheRestingTransforms();
        currentEmailIndex = 0;
        inputLocked = false;
        completionRequested = false;
        alertIcon.enabled = false;
        safeButton.enabled = true;
        maliciousButton.enabled = true;
        ShowCurrentEmail();
    }

    private void SubmitAnswer(EmailKind answer)
    {
        inputLocked = true;

        if (answer == emailSequence[currentEmailIndex])
        {
            StartCoroutine(ShowCorrectAnswer(answer));
            return;
        }

        StartCoroutine(ShowWrongAnswerWarning(answer));
    }

    private IEnumerator ShowCorrectAnswer(EmailKind answer)
    {
        ProceduralGameAudio.Play(GameSound.EmailSwipe, 0.035f);
        SpriteRenderer currentEmail = GetCurrentEmailRenderer();
        SpriteRenderer selectedButton = answer == EmailKind.Safe
            ? safeButton
            : maliciousButton;
        Vector3 buttonScale = answer == EmailKind.Safe
            ? safeButtonRestScale
            : maliciousButtonRestScale;

        StartCoroutine(MiniGameJuice.FlashColor(
            selectedButton,
            new Color(0.35f, 1f, 0.45f),
            0.28f,
            2));
        StartCoroutine(MiniGameJuice.PunchScale(
            selectedButton.transform,
            buttonScale,
            0.22f,
            0.26f));

        Vector3 destination = selectedButton.bounds.center;
        destination.z = currentEmail.transform.position.z;
        yield return MiniGameJuice.MoveScaleFade(
            currentEmail,
            currentEmail.transform.position,
            destination,
            currentEmail.transform.localScale,
            currentEmail.transform.localScale * 0.12f,
            0.28f);

        currentEmail.enabled = false;
        AdvanceToNextEmail();
    }

    private IEnumerator ShowWrongAnswerWarning(EmailKind answer)
    {
        MiniGamePerformanceSession.RegisterMistake();
        ProceduralGameAudio.Play(GameSound.WrongAction, 0.025f);
        SpriteRenderer currentEmail = GetCurrentEmailRenderer();
        SpriteRenderer selectedButton = answer == EmailKind.Safe
            ? safeButton
            : maliciousButton;
        Vector3 buttonScale = answer == EmailKind.Safe
            ? safeButtonRestScale
            : maliciousButtonRestScale;
        Bounds currentBounds = currentEmail.bounds;
        Vector3 alertPosition = currentBounds.center;
        alertPosition.z = alertIcon.transform.position.z;
        alertIcon.transform.position = alertPosition;
        alertIcon.sortingOrder = Mathf.Max(
            safeEmail.sortingOrder,
            maliciousEmail.sortingOrder) + 10;
        alertIcon.color = Color.white;
        alertIcon.enabled = true;

        StartCoroutine(MiniGameJuice.FlashColor(
            selectedButton,
            new Color(1f, 0.12f, 0.1f),
            0.3f,
            2));
        StartCoroutine(MiniGameJuice.PunchScale(
            selectedButton.transform,
            buttonScale,
            0.18f,
            0.24f));
        StartCoroutine(MiniGameJuice.ShakePosition(
            currentEmail.transform,
            GetCurrentEmailRestPosition(),
            0.12f,
            0.3f,
            50f));

        const float alertAnimationDuration = 0.2f;
        yield return MiniGameJuice.PopIn(
            alertIcon.transform,
            alertRestScale,
            alertAnimationDuration,
            1.25f);

        float remainingLockSeconds = Mathf.Max(
            0f,
            wrongAnswerLockSeconds - alertAnimationDuration);
        if (remainingLockSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(remainingLockSeconds);
        }

        alertIcon.enabled = false;
        inputLocked = false;
    }

    private void AdvanceToNextEmail()
    {
        currentEmailIndex++;

        if (currentEmailIndex >= emailSequence.Count)
        {
            CompleteGame();
            return;
        }

        ShowCurrentEmail();
    }

    private void ShowCurrentEmail()
    {
        ResetEmailRenderer(
            safeEmail,
            safeEmailRestPosition,
            safeEmailRestScale);
        ResetEmailRenderer(
            maliciousEmail,
            maliciousEmailRestPosition,
            maliciousEmailRestScale);

        bool showSafe = emailSequence[currentEmailIndex] == EmailKind.Safe;
        safeEmail.enabled = showSafe;
        maliciousEmail.enabled = !showSafe;
        StartCoroutine(ShowEmailEntrance(GetCurrentEmailRenderer()));
    }

    private IEnumerator ShowEmailEntrance(SpriteRenderer email)
    {
        inputLocked = true;
        yield return MiniGameJuice.PopIn(
            email.transform,
            GetCurrentEmailRestScale(),
            0.23f,
            1.18f);

        if (!completionRequested)
        {
            inputLocked = false;
        }
    }

    private void CompleteGame()
    {
        if (completionRequested)
            return;

        completionRequested = true;
        inputLocked = true;
        alertIcon.enabled = false;
        ProceduralGameAudio.Play(GameSound.TaskCompleted);

        string activeSceneName = SceneManager.GetActiveScene().name;
        TaskMissionSession.CompleteLaunchedTaskForScene(activeSceneName);

        if (string.IsNullOrWhiteSpace(completionSceneName))
        {
            Debug.LogError("E-posta mini game completion scene is missing.", this);
            return;
        }

        SceneManager.LoadScene(completionSceneName);
    }

    private SpriteRenderer GetCurrentEmailRenderer()
    {
        return emailSequence[currentEmailIndex] == EmailKind.Safe
            ? safeEmail
            : maliciousEmail;
    }

    private Vector3 GetCurrentEmailRestPosition()
    {
        return emailSequence[currentEmailIndex] == EmailKind.Safe
            ? safeEmailRestPosition
            : maliciousEmailRestPosition;
    }

    private Vector3 GetCurrentEmailRestScale()
    {
        return emailSequence[currentEmailIndex] == EmailKind.Safe
            ? safeEmailRestScale
            : maliciousEmailRestScale;
    }

    private void CacheRestingTransforms()
    {
        safeEmailRestPosition = safeEmail.transform.position;
        maliciousEmailRestPosition = maliciousEmail.transform.position;
        safeEmailRestScale = safeEmail.transform.localScale;
        maliciousEmailRestScale = maliciousEmail.transform.localScale;
        safeButtonRestScale = safeButton.transform.localScale;
        maliciousButtonRestScale = maliciousButton.transform.localScale;
        alertRestScale = alertIcon.transform.localScale;
    }

    private static void ResetEmailRenderer(
        SpriteRenderer renderer,
        Vector3 restingPosition,
        Vector3 restingScale)
    {
        renderer.transform.position = restingPosition;
        renderer.transform.localScale = restingScale;
        renderer.color = Color.white;
    }

    private bool HasRequiredReferences()
    {
        return safeEmail != null &&
            maliciousEmail != null &&
            alertIcon != null &&
            safeButton != null &&
            maliciousButton != null;
    }

    private bool TryGetClickedAnswer(
        Vector2 screenPosition,
        out EmailKind answer)
    {
        answer = default;
        if (gameCamera == null)
        {
            return false;
        }

        float cameraDistance = Mathf.Abs(
            gameCamera.transform.position.z - safeButton.transform.position.z);
        Vector3 worldPosition = gameCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, cameraDistance));
        Vector2 pointerWorld = worldPosition;

        bool hitSafe = IsInsideButtonCircle(
            safeButton,
            pointerWorld,
            out float safeDistance);
        bool hitMalicious = IsInsideButtonCircle(
            maliciousButton,
            pointerWorld,
            out float maliciousDistance);

        if (!hitSafe && !hitMalicious)
            return false;

        answer = hitSafe && (!hitMalicious || safeDistance <= maliciousDistance)
            ? EmailKind.Safe
            : EmailKind.Malicious;
        return true;
    }

    private bool IsInsideButtonCircle(
        SpriteRenderer renderer,
        Vector2 worldPosition,
        out float distance)
    {
        distance = float.PositiveInfinity;
        if (renderer == null || !renderer.enabled)
            return false;

        Bounds bounds = renderer.bounds;
        Vector2 center = bounds.center;
        distance = Vector2.Distance(center, worldPosition);

        float radius = Mathf.Min(bounds.extents.x, bounds.extents.y) *
            Mathf.Clamp(buttonHitRadiusFactor, 0.2f, 1f);
        return distance <= radius;
    }

    private static void BuildBalancedRandomSequence(
        List<EmailKind> sequence,
        int count)
    {
        int safeCount = count / 2;
        int maliciousCount = count / 2;

        if (count % 2 != 0)
        {
            if (Random.value < 0.5f)
            {
                safeCount++;
            }
            else
            {
                maliciousCount++;
            }
        }

        for (int index = 0; index < safeCount; index++)
        {
            sequence.Add(EmailKind.Safe);
        }

        for (int index = 0; index < maliciousCount; index++)
        {
            sequence.Add(EmailKind.Malicious);
        }

        for (int index = sequence.Count - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            (sequence[index], sequence[swapIndex]) =
                (sequence[swapIndex], sequence[index]);
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
