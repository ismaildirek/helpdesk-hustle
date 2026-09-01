using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class PasswordEntryMiniGame : MonoBehaviour
{
    private sealed class KeyView
    {
        public char Character;
        public SpriteRenderer Renderer;
        public Vector3 BaseScale;
        public Coroutine Animation;
    }

    private const string AvailableCharacters = "12478ADFHKLNRST";

    [Header("Scene References")]
    [SerializeField] private SpriteRenderer backgroundRenderer = null;
    [SerializeField] private SpriteRenderer passwordPanel = null;
    [SerializeField] private SpriteRenderer userInputPanel = null;
    [SerializeField] private Font displayFont = null;

    [Header("Keyboard Assets")]
    [SerializeField] private Sprite[] keySprites = null;

    [Header("Rules")]
    [SerializeField, Min(1)] private int passwordLength = 8;
    [SerializeField] private string completionSceneName = "YeniOfis";

    [Header("Layout")]
    [SerializeField, Range(0.4f, 1.2f)] private float keyRenderedSize = 0.72f;
    [SerializeField, Range(0.02f, 0.15f)] private float textCharacterSize = 0.07f;

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float keyPressDuration = 0.12f;
    [SerializeField, Min(0.1f)] private float wrongShakeDuration = 0.38f;
    [SerializeField, Min(0.1f)] private float successDuration = 0.5f;

    private readonly List<KeyView> keys = new(AvailableCharacters.Length);
    private Camera gameCamera;
    private Transform passwordTextRoot;
    private Transform userInputTextRoot;
    private TextMesh[] passwordCharacters;
    private TextMesh[] userInputCharacters;
    private string targetPassword;
    private string enteredPassword = string.Empty;
    private bool inputLocked;
    private bool completionRequested;
    private Vector3 passwordPanelRestScale;
    private Vector3 userInputPanelRestScale;
    private Vector3 passwordTextRestScale;
    private Vector3 userInputTextRestScale;
    private Vector3 userInputPanelRestPosition;
    private Vector3 cameraRestPosition;
    private Coroutine cameraShakeRoutine;

    private void Awake()
    {
        gameCamera = Camera.main;

        if (!HasRequiredReferences())
        {
            Debug.LogError(
                "Password mini game is missing its camera, panels, font or key sprites.",
                this);
            enabled = false;
            return;
        }

        passwordLength = 8;
        CreateDisplays();
        CreateKeyboard();
        CachePresentationState();
        StartGame();
    }

    private void Update()
    {
        if (MiniGamePresentationSession.IsInputBlocked)
            return;

        if (inputLocked ||
            completionRequested ||
            !WasPointerPressed(out Vector2 screenPosition) ||
            !TryGetWorldPosition(screenPosition, out Vector2 worldPosition))
        {
            return;
        }

        KeyView key = FindKeyAt(worldPosition);
        if (key != null)
        {
            PressKey(key);
        }
    }

    private void StartGame()
    {
        targetPassword = GeneratePassword();
        enteredPassword = string.Empty;
        inputLocked = true;
        completionRequested = false;
        SetCharacterTexts(passwordCharacters, targetPassword);
        SetCharacterColor(
            passwordCharacters,
            new Color32(255, 236, 240, 255));
        RefreshInputDisplay();
        StartCoroutine(AnimateEntrance());
    }

    private void PressKey(KeyView key)
    {
        ProceduralGameAudio.Play(GameSound.KeyPress, 0.055f);

        if (key.Animation != null)
        {
            StopCoroutine(key.Animation);
            key.Renderer.transform.localScale = key.BaseScale;
        }

        key.Animation = StartCoroutine(AnimateKeyPress(key));
        StartCoroutine(MiniGameJuice.FlashColor(
            key.Renderer,
            new Color(0.4f, 0.9f, 1f),
            0.18f,
            1));
        enteredPassword += key.Character;
        RefreshInputDisplay();
        StartCoroutine(AnimateEnteredCharacter(enteredPassword.Length - 1));

        if (enteredPassword.Length < passwordLength)
        {
            return;
        }

        inputLocked = true;
        if (enteredPassword == targetPassword)
        {
            StartCoroutine(AnimateSuccessAndComplete());
        }
        else
        {
            StartCoroutine(AnimateWrongPassword());
        }
    }

    private void RefreshInputDisplay()
    {
        SetCharacterTexts(userInputCharacters, enteredPassword);
        SetCharacterColor(
            userInputCharacters,
            new Color32(99, 231, 255, 255));
    }

    private IEnumerator AnimateKeyPress(KeyView key)
    {
        float halfDuration = keyPressDuration * 0.5f;
        Vector3 pressedScale = key.BaseScale * 0.82f;

        yield return ScaleOverTime(
            key.Renderer.transform,
            key.BaseScale,
            pressedScale,
            halfDuration);
        yield return ScaleOverTime(
            key.Renderer.transform,
            pressedScale,
            key.BaseScale,
            halfDuration);

        key.Renderer.transform.localScale = key.BaseScale;
        key.Animation = null;
    }

    private IEnumerator AnimateEntrance()
    {
        Color backgroundColor = backgroundRenderer.color;
        StartCoroutine(MiniGameJuice.FadeSprite(
            backgroundRenderer,
            0f,
            backgroundColor.a,
            0.34f));
        StartCoroutine(MiniGameJuice.PopIn(
            passwordPanel.transform,
            passwordPanelRestScale,
            0.3f,
            1.16f));
        StartCoroutine(MiniGameJuice.PopIn(
            userInputPanel.transform,
            userInputPanelRestScale,
            0.32f,
            1.18f));
        StartCoroutine(MiniGameJuice.PopIn(
            passwordTextRoot,
            passwordTextRestScale,
            0.28f,
            1.14f));

        foreach (KeyView key in keys)
        {
            StartCoroutine(MiniGameJuice.PopIn(
                key.Renderer.transform,
                key.BaseScale,
                0.2f,
                1.18f));
            yield return new WaitForSecondsRealtime(0.025f);
        }

        yield return new WaitForSecondsRealtime(0.2f);
        inputLocked = false;
    }

    private IEnumerator AnimateEnteredCharacter(int characterIndex)
    {
        if (characterIndex < 0 ||
            characterIndex >= userInputCharacters.Length)
        {
            yield break;
        }

        Transform character = userInputCharacters[characterIndex].transform;
        yield return MiniGameJuice.PopIn(
            character,
            Vector3.one,
            0.14f,
            1.22f);
    }

    private IEnumerator AnimateWrongPassword()
    {
        MiniGamePerformanceSession.RegisterMistake();
        ProceduralGameAudio.Play(GameSound.WrongAction, 0.025f);
        Vector3 startPosition = userInputTextRoot.position;
        StartCoroutine(MiniGameJuice.FlashColor(
            userInputPanel,
            new Color(1f, 0.15f, 0.18f),
            wrongShakeDuration,
            3));
        StartCoroutine(MiniGameJuice.ShakePosition(
            userInputPanel.transform,
            userInputPanelRestPosition,
            0.055f,
            wrongShakeDuration,
            48f));
        ShakeCamera(0.025f, 0.18f);
        SetCharacterColor(
            userInputCharacters,
            new Color32(255, 68, 92, 255));

        float elapsed = 0f;
        while (elapsed < wrongShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / wrongShakeDuration);
            float offset = Mathf.Sin(progress * Mathf.PI * 8f) *
                0.14f * (1f - progress);
            userInputTextRoot.position =
                startPosition + Vector3.right * offset;
            yield return null;
        }

        userInputTextRoot.position = startPosition;
        enteredPassword = string.Empty;
        RefreshInputDisplay();
        ResetInputCharacterScales();
        inputLocked = false;
    }

    private IEnumerator AnimateSuccessAndComplete()
    {
        ProceduralGameAudio.Play(GameSound.TaskCompleted);
        SetCharacterColor(
            userInputCharacters,
            new Color32(96, 255, 142, 255));
        StartCoroutine(MiniGameJuice.FlashColor(
            userInputPanel,
            new Color(0.45f, 1f, 0.58f),
            0.46f,
            2));
        StartCoroutine(MiniGameJuice.FlashColor(
            passwordPanel,
            new Color(0.55f, 1f, 0.68f),
            0.46f,
            2));

        foreach (KeyView key in keys)
        {
            if (key.Animation != null)
            {
                StopCoroutine(key.Animation);
                key.Renderer.transform.localScale = key.BaseScale;
                key.Animation = null;
            }

            StartCoroutine(MiniGameJuice.FlashColor(
                key.Renderer,
                new Color(0.45f, 1f, 0.62f),
                0.3f,
                1));
            StartCoroutine(MiniGameJuice.PunchScale(
                key.Renderer.transform,
                key.BaseScale,
                0.12f,
                0.26f));
            yield return new WaitForSecondsRealtime(0.018f);
        }

        ShakeCamera(0.04f, 0.24f);
        Vector3 baseScale = userInputTextRoot.localScale;
        Vector3 pulseScale = baseScale * 1.16f;
        float halfDuration = successDuration * 0.5f;

        yield return ScaleOverTime(
            userInputTextRoot,
            baseScale,
            pulseScale,
            halfDuration);
        yield return ScaleOverTime(
            userInputTextRoot,
            pulseScale,
            baseScale,
            halfDuration);

        CompleteGame();
    }

    private void CachePresentationState()
    {
        passwordPanelRestScale = passwordPanel.transform.localScale;
        userInputPanelRestScale = userInputPanel.transform.localScale;
        passwordTextRestScale = passwordTextRoot.localScale;
        userInputTextRestScale = userInputTextRoot.localScale;
        userInputPanelRestPosition = userInputPanel.transform.position;
        cameraRestPosition = gameCamera.transform.position;
    }

    private void ResetInputCharacterScales()
    {
        foreach (TextMesh character in userInputCharacters)
        {
            character.transform.localScale = Vector3.one;
        }

        userInputTextRoot.localScale = userInputTextRestScale;
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

    private static IEnumerator ScaleOverTime(
        Transform target,
        Vector3 from,
        Vector3 to,
        float duration)
    {
        if (duration <= 0f)
        {
            target.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            target.localScale = Vector3.LerpUnclamped(
                from,
                to,
                SmoothStep(progress));
            yield return null;
        }

        target.localScale = to;
    }

    private void CompleteGame()
    {
        if (completionRequested)
        {
            return;
        }

        completionRequested = true;
        string activeSceneName = SceneManager.GetActiveScene().name;
        TaskMissionSession.CompleteLaunchedTaskForScene(activeSceneName);

        if (string.IsNullOrWhiteSpace(completionSceneName))
        {
            Debug.LogError(
                "Password mini game completion scene is missing.",
                this);
            return;
        }

        SceneManager.LoadScene(completionSceneName);
    }

    private void CreateDisplays()
    {
        passwordCharacters = CreateCharacterDisplay(
            "GeneratedPassword",
            passwordPanel,
            out passwordTextRoot,
            new Color32(255, 236, 240, 255),
            0.12f,
            0.88f);
        userInputCharacters = CreateCharacterDisplay(
            "EnteredPassword",
            userInputPanel,
            out userInputTextRoot,
            new Color32(99, 231, 255, 255),
            0.18f,
            0.83f);
    }

    private TextMesh[] CreateCharacterDisplay(
        string rootName,
        SpriteRenderer panel,
        out Transform root,
        Color color,
        float firstSlotCenter,
        float lastSlotCenter)
    {
        GameObject rootObject = new(rootName);
        rootObject.transform.SetParent(transform, false);
        Vector3 panelCenter = panel.bounds.center;
        rootObject.transform.position = new Vector3(
            panelCenter.x,
            panelCenter.y,
            -2f);
        root = rootObject.transform;

        TextMesh[] characters = new TextMesh[passwordLength];
        float halfWidth = panel.bounds.extents.x;

        for (int index = 0; index < characters.Length; index++)
        {
            GameObject characterObject = new($"Character_{index + 1}");
            characterObject.transform.SetParent(root, false);

            float normalizedSlot = Mathf.Lerp(
                firstSlotCenter,
                lastSlotCenter,
                index / (float)(characters.Length - 1));
            characterObject.transform.localPosition = new Vector3(
                Mathf.Lerp(-halfWidth, halfWidth, normalizedSlot),
                0f,
                0f);

            TextMesh text = characterObject.AddComponent<TextMesh>();
            text.font = displayFont;
            text.fontSize = 72;
            text.characterSize = textCharacterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.richText = false;

            MeshRenderer renderer =
                characterObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = displayFont.material;
            renderer.sortingOrder = 30;
            characters[index] = text;
        }

        return characters;
    }

    private static void SetCharacterTexts(
        TextMesh[] characterViews,
        string value)
    {
        for (int index = 0; index < characterViews.Length; index++)
        {
            characterViews[index].text = index < value.Length
                ? value[index].ToString()
                : string.Empty;
        }
    }

    private static void SetCharacterColor(
        TextMesh[] characterViews,
        Color color)
    {
        foreach (TextMesh characterView in characterViews)
        {
            characterView.color = color;
        }
    }

    private void CreateKeyboard()
    {
        Bounds bounds = backgroundRenderer.bounds;
        keys.Clear();

        for (int index = 0; index < AvailableCharacters.Length; index++)
        {
            GameObject keyObject = new($"Key_{AvailableCharacters[index]}");
            keyObject.transform.SetParent(transform, false);

            SpriteRenderer renderer = keyObject.AddComponent<SpriteRenderer>();
            renderer.sprite = keySprites[index];
            renderer.sortingOrder = 20;
            renderer.color = Color.white;
            SetRenderedSize(renderer, keyRenderedSize);

            int column = index % 5;
            int row = index / 5;
            float normalizedX = Mathf.Lerp(0.18f, 0.82f, column / 4f);
            float normalizedY = 0.31f - row * 0.105f;
            keyObject.transform.position = new Vector3(
                Mathf.Lerp(bounds.min.x, bounds.max.x, normalizedX),
                Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedY),
                -2f);

            keys.Add(new KeyView
            {
                Character = AvailableCharacters[index],
                Renderer = renderer,
                BaseScale = keyObject.transform.localScale
            });
        }
    }

    private KeyView FindKeyAt(Vector2 worldPosition)
    {
        foreach (KeyView key in keys)
        {
            Bounds bounds = key.Renderer.bounds;
            if (bounds.Contains(new Vector3(
                    worldPosition.x,
                    worldPosition.y,
                    bounds.center.z)))
            {
                return key;
            }
        }

        return null;
    }

    private string GeneratePassword()
    {
        char[] generated = new char[passwordLength];
        for (int index = 0; index < generated.Length; index++)
        {
            generated[index] = AvailableCharacters[
                Random.Range(0, AvailableCharacters.Length)];
        }

        return new string(generated);
    }

    private bool HasRequiredReferences()
    {
        if (gameCamera == null ||
            backgroundRenderer == null ||
            passwordPanel == null ||
            userInputPanel == null ||
            displayFont == null ||
            keySprites == null ||
            keySprites.Length != AvailableCharacters.Length)
        {
            return false;
        }

        foreach (Sprite sprite in keySprites)
        {
            if (sprite == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryGetWorldPosition(
        Vector2 screenPosition,
        out Vector2 worldPosition)
    {
        float cameraDistance = Mathf.Abs(
            gameCamera.transform.position.z - transform.position.z);
        Vector3 converted = gameCamera.ScreenToWorldPoint(new Vector3(
            screenPosition.x,
            screenPosition.y,
            cameraDistance));
        worldPosition = converted;
        return true;
    }

    private static void SetRenderedSize(
        SpriteRenderer renderer,
        float maximumSize)
    {
        Vector2 size = renderer.sprite.bounds.size;
        float largestSide = Mathf.Max(size.x, size.y);
        float scale = maximumSize / Mathf.Max(0.001f, largestSide);
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
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
