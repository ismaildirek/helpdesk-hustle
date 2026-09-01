using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CableGameWorldController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] leftHeads;
    [SerializeField] private SpriteRenderer[] rightHeads;
    [SerializeField] private CableGameManager manager;
    [SerializeField] private float cableWidth = 0.1f;

    private readonly List<CableConnection> connections = new();
    private CableConnection activeConnection;
    private Camera gameCamera;
    private Material cableMaterial;
    private Coroutine cameraShakeRoutine;
    private Vector3 cameraRestingPosition;

    private static readonly string[] ColorNames =
    {
        "Blue",
        "Green",
        "Orange",
        "Purple",
        "Yellow",
        "Red"
    };

    private static readonly Color[] CableColors =
    {
        new(0.08f, 0.55f, 1f),
        new(0.1f, 0.95f, 0.15f),
        new(1f, 0.42f, 0.04f),
        new(0.7f, 0.18f, 1f),
        new(1f, 0.84f, 0.04f),
        new(1f, 0.08f, 0.08f)
    };

    private void Start()
    {
        gameCamera = Camera.main;
        if (gameCamera != null)
        {
            cameraRestingPosition = gameCamera.transform.position;
        }

        BuildConnections();
    }

    private void Update()
    {
        if (MiniGamePresentationSession.IsInputBlocked)
            return;

        if (gameCamera == null ||
            (manager != null && manager.IsComplete) ||
            !TryReadPointer(
                out Vector2 pointerPosition,
                out bool pressed,
                out bool held,
                out bool released))
        {
            return;
        }

        if (pressed && activeConnection == null)
        {
            activeConnection = FindSourceAt(pointerPosition);

            if (activeConnection != null)
            {
                ProceduralGameAudio.Play(
                    GameSound.CablePickup,
                    0.04f);
                activeConnection.Line.enabled = true;
                UpdateActiveLine(pointerPosition);
                StartCoroutine(MiniGameJuice.PunchScale(
                    activeConnection.Source.transform,
                    activeConnection.SourceRestingScale,
                    0.16f,
                    0.18f));
            }
        }

        if (held && activeConnection != null)
        {
            UpdateActiveLine(pointerPosition);
        }

        if (released && activeConnection != null)
        {
            FinishDrag(pointerPosition);
        }
    }

    private void OnDisable()
    {
        if (activeConnection != null &&
            !activeConnection.Connected)
        {
            activeConnection.Line.enabled = false;
        }

        activeConnection = null;

        if (gameCamera != null)
        {
            gameCamera.transform.position = cameraRestingPosition;
        }
    }

    private void OnDestroy()
    {
        if (cableMaterial != null)
        {
            Destroy(cableMaterial);
        }
    }

    public void Configure(
        SpriteRenderer[] newLeftHeads,
        SpriteRenderer[] newRightHeads,
        CableGameManager newManager)
    {
        leftHeads = newLeftHeads;
        rightHeads = newRightHeads;
        manager = newManager;
    }

    private void BuildConnections()
    {
        connections.Clear();

        if (leftHeads == null ||
            rightHeads == null ||
            leftHeads.Length != ColorNames.Length ||
            rightHeads.Length != ColorNames.Length)
        {
            Debug.LogError(
                "Cable Game needs six left and six right cable heads.",
                this);
            return;
        }

        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader == null)
        {
            Debug.LogError(
                "Cable Game could not find the Sprites/Default shader.",
                this);
            return;
        }

        cableMaterial = new Material(spriteShader)
        {
            name = "Runtime Cable Material"
        };

        for (int index = 0;
             index < ColorNames.Length;
             index++)
        {
            SpriteRenderer source = leftHeads[index];
            SpriteRenderer target = rightHeads[index];

            if (source == null || target == null)
            {
                Debug.LogError(
                    $"Cable Game references are missing for {ColorNames[index]}.",
                    this);
                continue;
            }

            connections.Add(new CableConnection
            {
                Name = ColorNames[index],
                Source = source,
                Target = target,
                SourceRestingPosition = source.transform.position,
                TargetRestingPosition = target.transform.position,
                SourceRestingScale = source.transform.localScale,
                TargetRestingScale = target.transform.localScale,
                Line = CreateCableLine(
                    ColorNames[index],
                    CableColors[index],
                    source,
                    target)
            });
        }
    }

    private CableConnection FindSourceAt(Vector2 screenPosition)
    {
        Vector3 worldPosition = ScreenToWorld(screenPosition);

        foreach (CableConnection connection in connections)
        {
            if (connection.Connected || connection.FeedbackLocked)
            {
                continue;
            }

            Bounds hitBounds = connection.Source.bounds;
            hitBounds.Expand(0.16f);

            if (hitBounds.Contains(
                    new Vector3(
                        worldPosition.x,
                        worldPosition.y,
                        hitBounds.center.z)))
            {
                return connection;
            }
        }

        return null;
    }

    private void UpdateActiveLine(Vector2 pointerScreenPosition)
    {
        SetLine(
            activeConnection.Line,
            activeConnection.Source.bounds.center,
            ScreenToWorld(pointerScreenPosition));
    }

    private void FinishDrag(Vector2 pointerScreenPosition)
    {
        Vector3 worldPosition = ScreenToWorld(pointerScreenPosition);
        Bounds targetBounds = activeConnection.Target.bounds;
        float targetPadding = Mathf.Max(
            0.06f,
            0.18f - GameProgressionSession.DifficultyLevel * 0.018f);
        targetBounds.Expand(targetPadding);

        bool correctTarget = targetBounds.Contains(
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                targetBounds.center.z));

        if (correctTarget)
        {
            ProceduralGameAudio.Play(
                GameSound.CableConnected,
                0.035f);
            activeConnection.Connected = true;
            SetLine(
                activeConnection.Line,
                activeConnection.Source.bounds.center,
                activeConnection.Target.bounds.center);
            manager?.RegisterConnection();
            StartCoroutine(ShowCorrectConnection(activeConnection));

            if (manager != null && manager.IsComplete)
            {
                StartCoroutine(ShowCompletionWave());
            }

            Debug.Log(
                $"Cable connected: {activeConnection.Name}");
        }
        else
        {
            MiniGamePerformanceSession.RegisterMistake();
            ProceduralGameAudio.Play(GameSound.WrongAction, 0.025f);
            activeConnection.Line.enabled = false;
            StartCoroutine(ShowWrongConnection(activeConnection));
        }

        activeConnection = null;
    }

    private IEnumerator ShowCorrectConnection(CableConnection connection)
    {
        connection.FeedbackLocked = true;
        StartCoroutine(MiniGameJuice.FlashColor(
            connection.Source,
            Color.white,
            0.3f,
            2));
        StartCoroutine(MiniGameJuice.FlashColor(
            connection.Target,
            Color.white,
            0.3f,
            2));
        StartCoroutine(MiniGameJuice.PunchScale(
            connection.Source.transform,
            connection.SourceRestingScale,
            0.24f,
            0.28f));
        yield return MiniGameJuice.PunchScale(
            connection.Target.transform,
            connection.TargetRestingScale,
            0.24f,
            0.28f);
        connection.FeedbackLocked = false;
    }

    private IEnumerator ShowWrongConnection(CableConnection connection)
    {
        connection.FeedbackLocked = true;
        StartCoroutine(MiniGameJuice.FlashColor(
            connection.Source,
            new Color(1f, 0.15f, 0.12f),
            0.28f,
            2));
        yield return MiniGameJuice.ShakePosition(
            connection.Source.transform,
            connection.SourceRestingPosition,
            0.08f,
            0.24f,
            52f);
        connection.FeedbackLocked = false;
    }

    private IEnumerator ShowCompletionWave()
    {
        for (int index = 0; index < connections.Count; index++)
        {
            CableConnection connection = connections[index];
            StartCoroutine(MiniGameJuice.PunchScale(
                connection.Source.transform,
                connection.SourceRestingScale,
                0.18f,
                0.3f));
            StartCoroutine(MiniGameJuice.PunchScale(
                connection.Target.transform,
                connection.TargetRestingScale,
                0.18f,
                0.3f));
            yield return new WaitForSecondsRealtime(0.045f);
        }

        if (gameCamera != null)
        {
            if (cameraShakeRoutine != null)
            {
                StopCoroutine(cameraShakeRoutine);
                gameCamera.transform.position = cameraRestingPosition;
            }

            cameraShakeRoutine = StartCoroutine(
                MiniGameJuice.ShakePosition(
                    gameCamera.transform,
                    cameraRestingPosition,
                    0.045f,
                    0.28f,
                    46f));
        }
    }

    private LineRenderer CreateCableLine(
        string colorName,
        Color color,
        SpriteRenderer source,
        SpriteRenderer target)
    {
        GameObject lineObject = new($"ConnectedCable_{colorName}");
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = cableWidth;
        line.endWidth = cableWidth;
        line.numCapVertices = 6;
        line.numCornerVertices = 4;
        line.sharedMaterial = cableMaterial;
        line.startColor = color;
        line.endColor = color;
        line.sortingLayerID = source.sortingLayerID;
        line.sortingOrder =
            Mathf.Max(source.sortingOrder, target.sortingOrder) + 1;
        line.enabled = false;
        return line;
    }

    private static void SetLine(
        LineRenderer line,
        Vector3 start,
        Vector3 end)
    {
        start.z = -0.1f;
        end.z = -0.1f;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        float cameraDistance = Mathf.Abs(
            gameCamera.transform.position.z);

        Vector3 worldPosition = gameCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                cameraDistance));

        worldPosition.z = 0f;
        return worldPosition;
    }

    private static bool TryReadPointer(
        out Vector2 position,
        out bool pressed,
        out bool held,
        out bool released)
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            pressed = touch.press.wasPressedThisFrame;
            held = touch.press.isPressed;
            released = touch.press.wasReleasedThisFrame;

            if (pressed || held || released)
            {
                position = touch.position.ReadValue();
                return true;
            }
        }

        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            pressed = Mouse.current.leftButton.wasPressedThisFrame;
            held = Mouse.current.leftButton.isPressed;
            released = Mouse.current.leftButton.wasReleasedThisFrame;
            return pressed || held || released;
        }

        position = Vector2.zero;
        pressed = false;
        held = false;
        released = false;
        return false;
    }

    private sealed class CableConnection
    {
        public string Name;
        public SpriteRenderer Source;
        public SpriteRenderer Target;
        public LineRenderer Line;
        public bool Connected;
        public bool FeedbackLocked;
        public Vector3 SourceRestingPosition;
        public Vector3 TargetRestingPosition;
        public Vector3 SourceRestingScale;
        public Vector3 TargetRestingScale;
    }
}
