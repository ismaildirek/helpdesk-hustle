using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BrokenPcRepairController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer brokenCase;
    [SerializeField] private SpriteRenderer repairedCase;
    [SerializeField] private SpriteRenderer hand;
    [SerializeField] private SpriteRenderer hitEffect;
    [SerializeField] private Sprite[] hitEffectSprites;
    [SerializeField] private int requiredHits = 5;
    [SerializeField] private float hitAnimationDuration = 0.32f;
    [SerializeField] private float completionDelay = 0.8f;
    [SerializeField] private string completionSceneName = "YeniOfis";

    private Camera gameCamera;
    private Vector3 handRestPosition;
    private Vector3 handImpactPosition;
    private Vector3 caseRestPosition;
    private Vector3 caseRestScale;
    private Vector3 repairedCaseRestScale;
    private Vector3 handRestScale;
    private Vector3 hitEffectRestScale;
    private Quaternion handRestRotation;
    private Quaternion handImpactRotation;
    private Vector3 cameraRestPosition;
    private Coroutine cameraShakeRoutine;
    private GUIStyle counterStyle;
    private float animationTime;
    private float completionTime;
    private int hitCount;
    private bool animatingHit;
    private bool impactApplied;
    private bool repaired;
    private bool inputReady;

    private void Start()
    {
        gameCamera = Camera.main;
        handRestPosition = hand.transform.position;
        handRestScale = hand.transform.localScale;
        handRestRotation = hand.transform.rotation;
        handImpactRotation = handRestRotation *
            Quaternion.Euler(0f, 0f, -16f);
        caseRestPosition = brokenCase.transform.position;
        caseRestScale = brokenCase.transform.localScale;
        repairedCaseRestScale = repairedCase.transform.localScale;
        hitEffectRestScale = hitEffect.transform.localScale;

        if (gameCamera != null)
        {
            cameraRestPosition = gameCamera.transform.position;
        }

        Bounds caseBounds = brokenCase.bounds;
        handImpactPosition = new Vector3(
            caseBounds.center.x +
            caseBounds.extents.x * 0.35f,
            caseBounds.center.y +
            caseBounds.extents.y * 0.25f,
            handRestPosition.z);

        hitCount = 0;
        completionTime = 0f;
        repaired = false;
        animatingHit = false;
        inputReady = false;

        brokenCase.enabled = true;
        brokenCase.color = Color.white;
        repairedCase.enabled = false;
        repairedCase.color = Color.white;
        hand.enabled = true;
        hand.color = Color.white;
        hitEffect.enabled = false;
        StartCoroutine(AnimateEntrance());
    }

    private void Update()
    {
        if (MiniGamePresentationSession.IsInputBlocked)
            return;

        if (repaired)
        {
            completionTime += Time.deltaTime;

            if (completionTime >= completionDelay &&
                !string.IsNullOrWhiteSpace(completionSceneName))
            {
                SceneManager.LoadScene(completionSceneName);
            }

            return;
        }

        if (inputReady &&
            !animatingHit &&
            WasPointerPressed(out Vector2 screenPosition) &&
            IsPointerOverBrokenCase(screenPosition))
        {
            BeginHit();
        }

        if (animatingHit)
        {
            AnimateHit();
        }
    }

    public void Configure(
        SpriteRenderer newBrokenCase,
        SpriteRenderer newRepairedCase,
        SpriteRenderer newHand,
        SpriteRenderer newHitEffect,
        Sprite[] newHitEffectSprites,
        string newCompletionSceneName = "YeniOfis")
    {
        brokenCase = newBrokenCase;
        repairedCase = newRepairedCase;
        hand = newHand;
        hitEffect = newHitEffect;
        hitEffectSprites = newHitEffectSprites;
        completionSceneName = newCompletionSceneName;
    }

    private void BeginHit()
    {
        animationTime = 0f;
        impactApplied = false;
        animatingHit = true;
        hitEffect.enabled = false;
    }

    private IEnumerator AnimateEntrance()
    {
        StartCoroutine(MiniGameJuice.PopIn(
            hand.transform,
            handRestScale,
            0.24f,
            1.16f));
        yield return MiniGameJuice.PopIn(
            brokenCase.transform,
            caseRestScale,
            0.3f,
            1.18f);
        inputReady = true;
    }

    private void AnimateHit()
    {
        animationTime += Time.deltaTime;
        float progress = Mathf.Clamp01(
            animationTime / hitAnimationDuration);

        const float impactMoment = 0.45f;

        if (progress <= impactMoment)
        {
            float moveProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress / impactMoment);

            hand.transform.position =
                Vector3.Lerp(
                    handRestPosition,
                    handImpactPosition,
                    moveProgress);
            hand.transform.rotation = Quaternion.Lerp(
                handRestRotation,
                handImpactRotation,
                moveProgress);
        }
        else
        {
            if (!impactApplied)
            {
                ApplyImpact();
            }

            float returnProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    (progress - impactMoment) /
                    (1f - impactMoment));

            hand.transform.position =
                Vector3.Lerp(
                    handImpactPosition,
                    handRestPosition,
                    returnProgress);
            hand.transform.rotation = Quaternion.Lerp(
                handImpactRotation,
                handRestRotation,
                returnProgress);

            if (progress < 0.86f)
            {
                Vector2 shake =
                    Random.insideUnitCircle * 0.07f;

                brokenCase.transform.position =
                    caseRestPosition +
                    new Vector3(shake.x, shake.y, 0f);
            }
            else
            {
                brokenCase.transform.position =
                    caseRestPosition;
                hitEffect.enabled = false;
            }
        }

        if (progress < 1f)
        {
            return;
        }

        hand.transform.position = handRestPosition;
        hand.transform.rotation = handRestRotation;
        brokenCase.transform.position = caseRestPosition;
        hitEffect.enabled = false;
        animatingHit = false;

        if (hitCount >= requiredHits)
        {
            CompleteRepair();
        }
    }

    private void ApplyImpact()
    {
        impactApplied = true;
        hitCount = Mathf.Min(hitCount + 1, requiredHits);
        ProceduralGameAudio.Play(GameSound.RepairHit, 0.08f);

        if (hitEffectSprites != null &&
            hitEffectSprites.Length > 0)
        {
            int effectIndex = Mathf.Clamp(
                Mathf.CeilToInt(
                    hitCount /
                    (float)requiredHits *
                    hitEffectSprites.Length) - 1,
                0,
                hitEffectSprites.Length - 1);

            hitEffect.sprite =
                hitEffectSprites[effectIndex];
        }

        hitEffect.transform.position =
            new Vector3(
                handImpactPosition.x,
                handImpactPosition.y,
                hitEffect.transform.position.z);
        hitEffect.transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                Random.Range(-18f, 18f));
        hitEffect.transform.localScale = hitEffectRestScale;
        hitEffect.color = Color.white;
        hitEffect.enabled = true;

        StartCoroutine(MiniGameJuice.PopIn(
            hitEffect.transform,
            hitEffectRestScale,
            0.13f,
            1.3f));
        StartCoroutine(MiniGameJuice.FlashColor(
            brokenCase,
            new Color(1f, 0.72f, 0.35f),
            0.2f,
            2));
        StartCoroutine(MiniGameJuice.PunchScale(
            brokenCase.transform,
            caseRestScale,
            0.13f,
            0.18f));
        ShakeCamera(0.035f, 0.15f);
    }

    private void CompleteRepair()
    {
        repaired = true;
        ProceduralGameAudio.Play(GameSound.TaskCompleted);
        inputReady = false;
        completionTime = 0f;
        TaskMissionSession.CompleteLaunchedTaskForScene(
            SceneManager.GetActiveScene().name);
        hand.enabled = false;
        hitEffect.enabled = false;
        StartCoroutine(AnimateRepairComplete());
    }

    private IEnumerator AnimateRepairComplete()
    {
        repairedCase.transform.localScale = repairedCaseRestScale;
        repairedCase.color = Color.white;
        repairedCase.enabled = true;

        StartCoroutine(MiniGameJuice.FadeSprite(
            brokenCase,
            brokenCase.color.a,
            0f,
            0.26f,
            true));
        StartCoroutine(MiniGameJuice.FadeSprite(
            repairedCase,
            0f,
            1f,
            0.32f));
        ShakeCamera(0.055f, 0.26f);

        yield return MiniGameJuice.PopIn(
            repairedCase.transform,
            repairedCaseRestScale,
            0.36f,
            1.2f);

        StartCoroutine(MiniGameJuice.FlashColor(
            repairedCase,
            new Color(0.55f, 1f, 0.62f),
            0.34f,
            2));
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
                54f));
    }

    private void OnDisable()
    {
        if (gameCamera != null)
        {
            gameCamera.transform.position = cameraRestPosition;
        }

        if (hand != null)
        {
            hand.transform.position = handRestPosition;
            hand.transform.rotation = handRestRotation;
        }

        if (brokenCase != null)
        {
            brokenCase.transform.position = caseRestPosition;
            brokenCase.transform.localScale = caseRestScale;
        }
    }

    private bool IsPointerOverBrokenCase(
        Vector2 screenPosition)
    {
        if (gameCamera == null ||
            brokenCase == null ||
            !brokenCase.enabled)
        {
            return false;
        }

        float cameraDistance = Mathf.Abs(
            gameCamera.transform.position.z -
            brokenCase.transform.position.z);

        Vector3 worldPosition =
            gameCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    cameraDistance));

        Bounds bounds = brokenCase.bounds;
        return bounds.Contains(
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                bounds.center.z));
    }

    private void OnGUI()
    {
        if (counterStyle == null)
        {
            counterStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(
                    24,
                    Mathf.RoundToInt(Screen.height * 0.03f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        string message = repaired
            ? "REPAIRED!"
            : $"HITS: {hitCount}/{requiredHits}";

        GUI.Label(
            new Rect(
                0f,
                Mathf.Max(45f, Screen.height * 0.04f),
                Screen.width,
                Mathf.Max(60f, Screen.height * 0.05f)),
            message,
            counterStyle);
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
