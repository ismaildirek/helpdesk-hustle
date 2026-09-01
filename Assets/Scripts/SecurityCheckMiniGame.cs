using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SecurityCheckMiniGame : MonoBehaviour
{
    private enum CardType
    {
        Green,
        Amber,
        Red,
        Damaged
    }

    private const int CardCount = 6;

    [SerializeField] private SpriteRenderer currentCard;
    [SerializeField] private Sprite[] cardSprites;
    [SerializeField] private SpriteRenderer approveIcon;
    [SerializeField] private SpriteRenderer rejectIcon;
    [SerializeField] private SpriteRenderer scannerFrame;
    [SerializeField] private SpriteRenderer closedLock;
    [SerializeField] private SpriteRenderer openLock;
    [SerializeField] private SpriteRenderer alertBeacon;
    [SerializeField] private TextMesh progressText;
    [SerializeField] private TextMesh hintText;
    [SerializeField] private string completionSceneName = "YeniOfis";

    private readonly CardType[] deck = new CardType[CardCount];

    private Camera gameCamera;
    private int cardIndex;
    private CardType currentType;
    private bool amberScanned;
    private bool amberApproves;
    private bool inputLocked;
    private bool completing;
    private float idleClock;
    private Vector3 cardRestPosition;
    private Vector3 cardRestScale;
    private Vector3 scannerRestScale;

    private void Awake()
    {
        gameCamera = Camera.main;
        if (currentCard != null)
        {
            cardRestPosition = currentCard.transform.position;
            cardRestScale = currentCard.transform.localScale;
        }

        if (scannerFrame != null)
            scannerRestScale = scannerFrame.transform.localScale;
        if (alertBeacon != null)
            alertBeacon.enabled = false;
        if (openLock != null)
            openLock.enabled = false;
        if (closedLock != null)
            closedLock.enabled = true;

        BuildDeck();
        ShowCurrentCard();
    }

    private void Update()
    {
        AnimateIdle();

        if (MiniGamePresentationSession.IsInputBlocked ||
            GamePauseSession.IsPaused || inputLocked || completing ||
            !MiniGamePointerInput.WasPressed(out Vector2 screenPosition) ||
            !MiniGamePointerInput.TryGetWorldPosition(
                gameCamera,
                screenPosition,
                out Vector2 worldPosition))
        {
            return;
        }

        if (MiniGamePointerInput.IsNear(
                currentCard, worldPosition, 0.92f))
        {
            if (currentType == CardType.Amber && !amberScanned)
                StartCoroutine(ScanAmberCard());
            return;
        }

        if (MiniGamePointerInput.IsNear(
                approveIcon, worldPosition, 0.72f))
        {
            EvaluateDecision(true);
            return;
        }

        if (MiniGamePointerInput.IsNear(
                rejectIcon, worldPosition, 0.72f))
        {
            EvaluateDecision(false);
        }
    }

    private void AnimateIdle()
    {
        idleClock += Time.unscaledDeltaTime;
        if (scannerFrame != null)
        {
            float pulse = 1f + Mathf.Sin(idleClock * 4.4f) * 0.035f;
            scannerFrame.transform.localScale = scannerRestScale * pulse;
            Color color = scannerFrame.color;
            color.a = 0.62f + Mathf.Sin(idleClock * 4.4f) * 0.22f;
            scannerFrame.color = color;
        }
    }

    private void BuildDeck()
    {
        deck[0] = CardType.Green;
        deck[1] = CardType.Amber;
        deck[2] = CardType.Red;
        deck[3] = CardType.Damaged;
        deck[4] = (CardType)UnityEngine.Random.Range(0, 4);
        deck[5] = (CardType)UnityEngine.Random.Range(0, 4);

        for (int index = deck.Length - 1; index > 0; index--)
        {
            int swapIndex = UnityEngine.Random.Range(0, index + 1);
            (deck[index], deck[swapIndex]) =
                (deck[swapIndex], deck[index]);
        }
    }

    private void ShowCurrentCard()
    {
        if (currentCard == null || cardSprites == null ||
            cardSprites.Length < 4)
        {
            Debug.LogError(
                "Security Check needs four card sprites.",
                this);
            return;
        }

        currentType = deck[cardIndex];
        amberScanned = false;
        amberApproves = false;
        currentCard.sprite = cardSprites[(int)currentType];
        currentCard.enabled = true;
        currentCard.transform.position = cardRestPosition;
        currentCard.transform.localScale = cardRestScale;
        currentCard.transform.rotation = Quaternion.identity;
        currentCard.color = Color.white;

        if (closedLock != null)
            closedLock.enabled = true;
        if (openLock != null)
            openLock.enabled = false;

        if (progressText != null)
            progressText.text = $"SECURITY CHECK  {cardIndex + 1}/{CardCount}";
        if (hintText != null)
        {
            hintText.text = currentType == CardType.Amber
                ? "TAP THE AMBER CARD TO SCAN"
                : "APPROVE OR REJECT THE BADGE";
        }

        StartCoroutine(MiniGameJuice.PopIn(
            currentCard.transform,
            cardRestScale,
            0.24f,
            1.14f));
    }

    private IEnumerator ScanAmberCard()
    {
        inputLocked = true;
        ProceduralGameAudio.Play(GameSound.SecurityScan, 0.018f);
        amberApproves = UnityEngine.Random.value >= 0.5f;

        if (scannerFrame != null)
        {
            StartCoroutine(MiniGameJuice.FlashColor(
                scannerFrame,
                new Color(0.2f, 1f, 1f, 1f),
                0.55f,
                3));
        }

        if (currentCard != null)
        {
            yield return MiniGameJuice.FlashColor(
                currentCard,
                new Color(0.3f, 0.95f, 1f, 1f),
                0.52f,
                3);
            currentCard.sprite = cardSprites[
                amberApproves
                    ? (int)CardType.Green
                    : (int)CardType.Red];
        }

        amberScanned = true;
        if (hintText != null)
        {
            hintText.text = amberApproves
                ? "SCAN CLEAR: APPROVE"
                : "SCAN FAILED: REJECT";
        }

        inputLocked = false;
    }

    private void EvaluateDecision(bool approved)
    {
        if (currentType == CardType.Amber && !amberScanned)
        {
            RegisterWrongDecision("SCAN AMBER CARDS FIRST");
            return;
        }

        bool shouldApprove = currentType switch
        {
            CardType.Green => true,
            CardType.Amber => amberApproves,
            _ => false
        };

        if (approved != shouldApprove)
        {
            RegisterWrongDecision("WRONG DECISION. TRY AGAIN.");
            return;
        }

        StartCoroutine(AcceptDecision(approved));
    }

    private IEnumerator AcceptDecision(bool approved)
    {
        inputLocked = true;
        ProceduralGameAudio.Play(GameSound.SecurityDecision, 0.02f);
        SpriteRenderer selectedIcon = approved ? approveIcon : rejectIcon;
        if (selectedIcon != null)
        {
            StartCoroutine(MiniGameJuice.PunchScale(
                selectedIcon.transform,
                selectedIcon.transform.localScale,
                0.19f,
                0.22f));
        }

        if (closedLock != null)
            closedLock.enabled = false;
        if (openLock != null)
        {
            openLock.enabled = true;
            StartCoroutine(MiniGameJuice.PopIn(
                openLock.transform,
                openLock.transform.localScale,
                0.2f,
                1.15f));
        }

        if (currentCard != null)
        {
            Vector3 end = cardRestPosition +
                new Vector3(approved ? 2.2f : -2.2f, 0.45f, 0f);
            yield return MiniGameJuice.MoveScaleFade(
                currentCard,
                cardRestPosition,
                end,
                cardRestScale,
                cardRestScale * 0.7f,
                0.34f);
        }

        cardIndex++;
        if (cardIndex >= CardCount)
        {
            yield return CompleteSecurityCheck();
            yield break;
        }

        yield return new WaitForSecondsRealtime(0.18f);
        ShowCurrentCard();
        inputLocked = false;
    }

    private IEnumerator CompleteSecurityCheck()
    {
        completing = true;
        if (progressText != null)
            progressText.text = "CHECKPOINT SECURE";
        if (hintText != null)
            hintText.text = "NO SUSPICIOUS RECTANGLES TODAY.";

        TaskMissionSession.CompleteLaunchedTaskForScene(
            SceneManager.GetActiveScene().name);
        ProceduralGameAudio.Play(GameSound.TaskCompleted);
        yield return new WaitForSecondsRealtime(0.95f);
        SceneManager.LoadScene(completionSceneName);
    }

    private void RegisterWrongDecision(string message)
    {
        MiniGamePerformanceSession.RegisterMistake();
        ProceduralGameAudio.Play(GameSound.WrongAction, 0.025f);
        if (hintText != null)
            hintText.text = message;
        if (currentCard != null)
        {
            StartCoroutine(MiniGameJuice.ShakePosition(
                currentCard.transform,
                cardRestPosition,
                0.12f,
                0.24f));
        }

        if (alertBeacon != null)
            StartCoroutine(ShowAlert());
    }

    private IEnumerator ShowAlert()
    {
        alertBeacon.enabled = true;
        Vector3 restScale = alertBeacon.transform.localScale;
        yield return MiniGameJuice.PopIn(
            alertBeacon.transform,
            restScale,
            0.18f,
            1.2f);
        yield return MiniGameJuice.FadeSprite(
            alertBeacon,
            1f,
            0f,
            0.36f,
            true);
        Color color = alertBeacon.color;
        color.a = 1f;
        alertBeacon.color = color;
    }

#if UNITY_EDITOR
    public void ConfigureEditor(
        SpriteRenderer configuredCurrentCard,
        Sprite[] configuredCardSprites,
        SpriteRenderer configuredApprove,
        SpriteRenderer configuredReject,
        SpriteRenderer configuredScanner,
        SpriteRenderer configuredClosedLock,
        SpriteRenderer configuredOpenLock,
        SpriteRenderer configuredAlert,
        TextMesh configuredProgress,
        TextMesh configuredHint)
    {
        currentCard = configuredCurrentCard;
        cardSprites = configuredCardSprites;
        approveIcon = configuredApprove;
        rejectIcon = configuredReject;
        scannerFrame = configuredScanner;
        closedLock = configuredClosedLock;
        openLock = configuredOpenLock;
        alertBeacon = configuredAlert;
        progressText = configuredProgress;
        hintText = configuredHint;
        completionSceneName = "YeniOfis";
    }
#endif
}
