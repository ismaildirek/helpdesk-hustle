using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class OfficeNavigationMenu : MonoBehaviour
{
    private const string OfficeSceneName = "YeniOfis";
    private const string NavigationObjectName = "navigasyon";
    private const string PauseObjectName = "durdur";
    private const string SoundMutedObjectName = "ses_k\u0131s\u0131k";
    private const string SoundOnObjectName = "ses_a\u00E7";
    private const string HelpObjectName = "soru_isareti";

    private Camera gameCamera;
    private SpriteRenderer navigationRenderer;
    private SpriteRenderer pauseRenderer;
    private SpriteRenderer soundMutedRenderer;
    private SpriteRenderer soundOnRenderer;
    private SpriteRenderer helpRenderer;
    private Sprite pauseSprite;
    private Sprite resumeSprite;
    private Vector3 navigationRestScale;
    private Vector3 pauseRestPosition;
    private Vector3 soundRestPosition;
    private Vector3 helpRestPosition;
    private Vector3 pauseRestScale;
    private Vector3 soundRestScale;
    private Vector3 helpRestScale;
    private Color pauseRestColor;
    private Color soundMutedRestColor;
    private Color soundOnRestColor;
    private Color helpRestColor;
    private Coroutine menuAnimation;
    private bool menuOpen;
    private bool menuAnimating;

    internal static bool HandleSystemBack()
    {
        OfficeNavigationMenu menu =
            FindFirstObjectByType<OfficeNavigationMenu>(
                FindObjectsInactive.Exclude);
        if (menu == null || !menu.enabled || menu.menuAnimating)
            return false;

        if (GamePauseSession.IsPaused)
        {
            GamePauseSession.SetPaused(false);
            menu.RefreshPauseIcon();
            return true;
        }

        menu.ToggleMenu();
        return true;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSceneSubscription()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneInstaller()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (scene.name != OfficeSceneName)
            return;

        GameObject navigation = GameObject.Find(NavigationObjectName);
        if (navigation != null &&
            navigation.GetComponent<OfficeNavigationMenu>() == null)
        {
            navigation.AddComponent<OfficeNavigationMenu>();
        }
    }

    private void Awake()
    {
        gameCamera = Camera.main;
        navigationRenderer = GetComponent<SpriteRenderer>();
        pauseRenderer = FindRenderer(PauseObjectName);
        soundMutedRenderer = FindRenderer(SoundMutedObjectName);
        soundOnRenderer = FindRenderer(SoundOnObjectName);
        helpRenderer = FindRenderer(HelpObjectName);

        if (gameCamera == null ||
            pauseRenderer == null ||
            soundMutedRenderer == null ||
            soundOnRenderer == null ||
            helpRenderer == null)
        {
            Debug.LogError(
                "Office navigation menu is missing its camera or icon objects.",
                this);
            enabled = false;
            return;
        }

        GamePresentationLibrary library =
            Resources.Load<GamePresentationLibrary>(
                "GamePresentationLibrary");
        resumeSprite = library != null
            ? library.ResumeIcon
            : null;
        pauseSprite = pauseRenderer.sprite;

        navigationRestScale = transform.localScale;
        pauseRestPosition = pauseRenderer.transform.position;
        soundRestPosition = soundMutedRenderer.transform.position;
        helpRestPosition = helpRenderer.transform.position;
        pauseRestScale = pauseRenderer.transform.localScale;
        soundRestScale = soundMutedRenderer.transform.localScale;
        helpRestScale = helpRenderer.transform.localScale;
        pauseRestColor = pauseRenderer.color;
        soundMutedRestColor = soundMutedRenderer.color;
        soundOnRestColor = soundOnRenderer.color;
        helpRestColor = helpRenderer.color;

        soundOnRenderer.transform.position = soundRestPosition;
        soundOnRenderer.transform.localScale = soundRestScale;
        SetMenuIconsVisible(false);
        RefreshPauseIcon();
        RefreshSoundIcon();
    }

    private void Update()
    {
        if (BossIntroDialogue.IsBlockingOfficeInput ||
            OfficeHelpOverlay.IsBlockingOfficeInput ||
            menuAnimating ||
            !WasPointerPressed(out Vector2 pointerPosition))
        {
            return;
        }

        if (IsPointerOver(navigationRenderer, pointerPosition))
        {
            PlayUiClick();
            StartCoroutine(MiniGameJuice.PunchScale(
                transform,
                navigationRestScale,
                0.12f,
                0.2f));
            ToggleMenu();
            return;
        }

        if (!menuOpen)
            return;

        if (IsPointerOver(pauseRenderer, pointerPosition))
        {
            PlayUiClick();
            GamePauseSession.Toggle();
            RefreshPauseIcon();
            StartCoroutine(MiniGameJuice.PunchScale(
                pauseRenderer.transform,
                pauseRestScale,
                0.16f,
                0.22f));
            return;
        }

        SpriteRenderer activeSoundRenderer =
            ProceduralGameAudio.IsMuted
                ? soundOnRenderer
                : soundMutedRenderer;
        if (IsPointerOver(activeSoundRenderer, pointerPosition))
        {
            bool unmuting = ProceduralGameAudio.IsMuted;
            if (unmuting)
            {
                ProceduralGameAudio.SetMuted(false);
                PlayUiClick();
            }
            else
            {
                PlayUiClick();
                ProceduralGameAudio.SetMuted(true);
            }

            RefreshSoundIcon();
            SpriteRenderer refreshedRenderer = unmuting
                ? soundMutedRenderer
                : soundOnRenderer;
            StartCoroutine(MiniGameJuice.PopIn(
                refreshedRenderer.transform,
                soundRestScale,
                0.2f,
                1.16f));
            return;
        }

        if (IsPointerOver(helpRenderer, pointerPosition))
        {
            PlayUiClick();
            StartCoroutine(MiniGameJuice.PunchScale(
                helpRenderer.transform,
                helpRestScale,
                0.12f,
                0.2f));
            OfficeHelpOverlay.Show();
        }
    }

    private void ToggleMenu()
    {
        menuOpen = !menuOpen;
        if (menuAnimation != null)
            StopCoroutine(menuAnimation);

        menuAnimation = StartCoroutine(AnimateMenu(menuOpen));
    }

    private IEnumerator AnimateMenu(bool opening)
    {
        menuAnimating = true;
        SpriteRenderer activeSoundRenderer =
            ProceduralGameAudio.IsMuted
                ? soundOnRenderer
                : soundMutedRenderer;
        SpriteRenderer[] renderers =
        {
            pauseRenderer,
            activeSoundRenderer,
            helpRenderer
        };
        Vector3[] positions =
        {
            pauseRestPosition,
            soundRestPosition,
            helpRestPosition
        };
        Vector3[] scales =
        {
            pauseRestScale,
            soundRestScale,
            helpRestScale
        };

        if (opening)
        {
            SetMenuIconsVisible(false);
            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];
                PrepareIconForOpening(renderer);
                StartCoroutine(AnimateIcon(
                    renderer,
                    transform.position,
                    positions[index],
                    Vector3.zero,
                    scales[index],
                    0f,
                    1f,
                    0.24f));
                yield return new WaitForSecondsRealtime(0.055f);
            }

            yield return new WaitForSecondsRealtime(0.22f);
        }
        else
        {
            for (int index = renderers.Length - 1;
                 index >= 0;
                 index--)
            {
                SpriteRenderer renderer = renderers[index];
                StartCoroutine(AnimateIcon(
                    renderer,
                    positions[index],
                    transform.position,
                    scales[index],
                    Vector3.zero,
                    1f,
                    0f,
                    0.2f));
                yield return new WaitForSecondsRealtime(0.045f);
            }

            yield return new WaitForSecondsRealtime(0.2f);
            RestoreIconPresentation();
            SetMenuIconsVisible(false);
        }

        menuAnimating = false;
        menuAnimation = null;
    }

    private IEnumerator AnimateIcon(
        SpriteRenderer renderer,
        Vector3 startPosition,
        Vector3 endPosition,
        Vector3 startScale,
        Vector3 endScale,
        float startAlpha,
        float endAlpha,
        float duration)
    {
        float elapsed = 0f;
        Color color = renderer.color;
        while (elapsed < duration && renderer != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration));
            renderer.transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                progress);
            renderer.transform.localScale = Vector3.Lerp(
                startScale,
                endScale,
                progress);
            color.a = Mathf.Lerp(startAlpha, endAlpha, progress);
            renderer.color = color;
            yield return null;
        }

        if (renderer != null)
        {
            renderer.transform.position = endPosition;
            renderer.transform.localScale = endScale;
            color.a = endAlpha;
            renderer.color = color;
        }
    }

    private void PrepareIconForOpening(SpriteRenderer renderer)
    {
        renderer.enabled = true;
        renderer.transform.position = transform.position;
        renderer.transform.localScale = Vector3.zero;
        Color color = renderer.color;
        color.a = 0f;
        renderer.color = color;
    }

    private void RefreshPauseIcon()
    {
        pauseRenderer.sprite = GamePauseSession.IsPaused &&
            resumeSprite != null
                ? resumeSprite
                : pauseSprite;
    }

    private void RefreshSoundIcon()
    {
        if (!menuOpen)
        {
            soundMutedRenderer.enabled = false;
            soundOnRenderer.enabled = false;
            return;
        }

        soundMutedRenderer.enabled = !ProceduralGameAudio.IsMuted;
        soundOnRenderer.enabled = ProceduralGameAudio.IsMuted;
    }

    private void SetMenuIconsVisible(bool visible)
    {
        pauseRenderer.enabled = visible;
        helpRenderer.enabled = visible;
        soundMutedRenderer.enabled =
            visible && !ProceduralGameAudio.IsMuted;
        soundOnRenderer.enabled =
            visible && ProceduralGameAudio.IsMuted;
    }

    private void RestoreIconPresentation()
    {
        RestoreRenderer(
            pauseRenderer,
            pauseRestPosition,
            pauseRestScale,
            pauseRestColor);
        RestoreRenderer(
            soundMutedRenderer,
            soundRestPosition,
            soundRestScale,
            soundMutedRestColor);
        RestoreRenderer(
            soundOnRenderer,
            soundRestPosition,
            soundRestScale,
            soundOnRestColor);
        RestoreRenderer(
            helpRenderer,
            helpRestPosition,
            helpRestScale,
            helpRestColor);
    }

    private bool IsPointerOver(
        SpriteRenderer renderer,
        Vector2 screenPosition)
    {
        if (renderer == null ||
            !renderer.enabled ||
            gameCamera == null)
        {
            return false;
        }

        float distance = Mathf.Abs(
            gameCamera.transform.position.z -
            renderer.transform.position.z);
        Vector3 worldPosition = gameCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distance));
        Bounds bounds = renderer.bounds;
        bounds.Expand(0.08f);
        return bounds.Contains(new Vector3(
            worldPosition.x,
            worldPosition.y,
            bounds.center.z));
    }

    private static SpriteRenderer FindRenderer(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target != null
            ? target.GetComponent<SpriteRenderer>()
            : null;
    }

    private static void RestoreRenderer(
        SpriteRenderer renderer,
        Vector3 position,
        Vector3 scale,
        Color color)
    {
        renderer.transform.position = position;
        renderer.transform.localScale = scale;
        renderer.color = color;
    }

    private static void PlayUiClick()
    {
        ProceduralGameAudio.Play(GameSound.UiClick, 0.025f);
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
