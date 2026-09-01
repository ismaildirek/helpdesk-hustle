using UnityEngine;

public class EmailTriageRuntimeBootstrap : MonoBehaviour
{
    private const string RootName = "EmailTriageSystem";

    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite safeEmailSprite;
    [SerializeField] private Sprite maliciousEmailSprite;
    [SerializeField] private Sprite safeButtonSprite;
    [SerializeField] private Sprite maliciousButtonSprite;
    [SerializeField] private Sprite alertIconSprite;

    private void Awake()
    {
        EnsureGameInScene(GetComponent<Camera>());
    }

    public void EnsureGameInScene(Camera gameCamera = null)
    {
        if (FindFirstObjectByType<EmailTriageMiniGame>(
                FindObjectsInactive.Include) != null)
        {
            return;
        }

        if (backgroundSprite == null || safeEmailSprite == null ||
            maliciousEmailSprite == null || safeButtonSprite == null ||
            maliciousButtonSprite == null || alertIconSprite == null)
        {
            Debug.LogError(
                "E-posta mini game bootstrap is missing one or more sprites.",
                this);
            return;
        }

        if (gameCamera == null)
        {
            gameCamera = GetComponent<Camera>();
        }

        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }

        if (gameCamera == null)
        {
            Debug.LogError("E-posta mini game needs a Main Camera.", this);
            return;
        }

        BuildGame(gameCamera);
    }

    public void Configure(
        Sprite newBackgroundSprite,
        Sprite newSafeEmailSprite,
        Sprite newMaliciousEmailSprite,
        Sprite newSafeButtonSprite,
        Sprite newMaliciousButtonSprite,
        Sprite newAlertIconSprite)
    {
        backgroundSprite = newBackgroundSprite;
        safeEmailSprite = newSafeEmailSprite;
        maliciousEmailSprite = newMaliciousEmailSprite;
        safeButtonSprite = newSafeButtonSprite;
        maliciousButtonSprite = newMaliciousButtonSprite;
        alertIconSprite = newAlertIconSprite;
    }

    private void BuildGame(Camera gameCamera)
    {
        GameObject root = new(RootName);
        root.SetActive(false);

        SpriteRenderer[] sceneRenderers = FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        SpriteRenderer background = FindRenderer(sceneRenderers, backgroundSprite);
        bool createdBackground = background == null;

        if (createdBackground)
        {
            background = CreateRenderer(
                "e_posta_arkaplan",
                root.transform,
                backgroundSprite,
                -100);
            background.transform.position = Vector3.zero;

            gameCamera.orthographic = true;
            gameCamera.transform.position = new Vector3(0f, 0f, -10f);
            gameCamera.orthographicSize = backgroundSprite.bounds.extents.y;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color32(22, 31, 49, 255);
        }

        Bounds backgroundBounds = background.bounds;

        SpriteRenderer safeEmail = FindRenderer(sceneRenderers, safeEmailSprite);
        SpriteRenderer maliciousEmail = FindRenderer(
            sceneRenderers,
            maliciousEmailSprite);

        if (safeEmail == null)
        {
            safeEmail = CreateRenderer(
                "iyi_e_posta",
                root.transform,
                safeEmailSprite,
                20);
            PlaceEmail(safeEmail, maliciousEmail, backgroundBounds);
        }

        if (maliciousEmail == null)
        {
            maliciousEmail = CreateRenderer(
                "kotu_e_posta",
                root.transform,
                maliciousEmailSprite,
                20);
            PlaceEmail(maliciousEmail, safeEmail, backgroundBounds);
        }

        SpriteRenderer safeButton = FindRenderer(sceneRenderers, safeButtonSprite);
        if (safeButton == null)
        {
            safeButton = CreateRenderer(
                "iyi_button",
                root.transform,
                safeButtonSprite,
                30);
            safeButton.transform.position = NormalizedToWorld(
                backgroundBounds,
                new Vector2(0.32f, 0.19f),
                -1f);
            SetRenderedHeight(safeButton, 8.5f);
        }

        SpriteRenderer maliciousButton = FindRenderer(
            sceneRenderers,
            maliciousButtonSprite);
        if (maliciousButton == null)
        {
            maliciousButton = CreateRenderer(
                "kotu_button",
                root.transform,
                maliciousButtonSprite,
                30);
            maliciousButton.transform.position = NormalizedToWorld(
                backgroundBounds,
                new Vector2(0.68f, 0.19f),
                -1f);
            SetRenderedHeight(maliciousButton, 8.5f);
        }

        SpriteRenderer alertIcon = FindRenderer(sceneRenderers, alertIconSprite);
        if (alertIcon == null)
        {
            alertIcon = CreateRenderer(
                "icon_alert",
                root.transform,
                alertIconSprite,
                100);
            alertIcon.transform.position = safeEmail.bounds.center;
            SetRenderedHeight(alertIcon, 2.1f);
        }

        alertIcon.sortingOrder = Mathf.Max(
            safeEmail.sortingOrder,
            maliciousEmail.sortingOrder) + 10;
        alertIcon.enabled = false;

        EmailTriageMiniGame controller = root.AddComponent<EmailTriageMiniGame>();
        controller.Configure(
            safeEmail,
            maliciousEmail,
            alertIcon,
            safeButton,
            maliciousButton,
            7,
            0.5f);

        root.SetActive(true);
    }

    private static void PlaceEmail(
        SpriteRenderer renderer,
        SpriteRenderer template,
        Bounds backgroundBounds)
    {
        if (template != null)
        {
            renderer.transform.SetPositionAndRotation(
                template.transform.position,
                template.transform.rotation);
            renderer.transform.localScale = template.transform.localScale;
            renderer.sortingOrder = template.sortingOrder;
            return;
        }

        renderer.transform.position = NormalizedToWorld(
            backgroundBounds,
            new Vector2(0.5f, 0.72f),
            -0.5f);
        SetRenderedHeight(renderer, 15f);
    }

    private static SpriteRenderer FindRenderer(
        SpriteRenderer[] renderers,
        Sprite sprite)
    {
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.sprite == sprite)
            {
                return renderer;
            }
        }

        return null;
    }

    private static SpriteRenderer CreateRenderer(
        string objectName,
        Transform parent,
        Sprite sprite,
        int sortingOrder)
    {
        GameObject gameObject = new(objectName);
        gameObject.transform.SetParent(parent, false);
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void SetRenderedHeight(SpriteRenderer renderer, float height)
    {
        float spriteHeight = Mathf.Max(0.001f, renderer.sprite.bounds.size.y);
        float scale = height / spriteHeight;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static Vector3 NormalizedToWorld(
        Bounds bounds,
        Vector2 normalizedPosition,
        float z)
    {
        return new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, normalizedPosition.x),
            Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedPosition.y),
            z);
    }
}
