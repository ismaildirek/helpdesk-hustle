using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class FileUploadController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer folderOpen;
    [SerializeField] private SpriteRenderer folderClosed;
    [SerializeField] private SpriteRenderer paperFile;
    [SerializeField] private SpriteRenderer progressBarEmpty;
    [SerializeField] private SpriteRenderer progressBarFull;
    [SerializeField] private SpriteRenderer uploadButton;
    [SerializeField] private float uploadDuration = 5f;
    [SerializeField] private float paperCycleDuration = 1.25f;
    [SerializeField] private string completionSceneName = "YeniOfis";

    private Camera gameCamera;
    private SpriteRenderer secondPaper;
    private Vector3 paperStart;
    private Vector3 paperEnd;
    private Vector3 progressBasePosition;
    private Vector3 progressBaseScale;
    private Vector3 paperRestScale;
    private Quaternion paperRestRotation;
    private Vector3 folderOpenRestScale;
    private Vector3 folderClosedRestScale;
    private Vector3 progressEmptyRestScale;
    private Vector3 uploadButtonRestScale;
    private float progressWidth;
    private float elapsed;
    private bool uploading;
    private bool foldersSharePosition;
    private bool completionRequested;
    private bool inputReady;
    private int lastProgressStep;

    private void Start()
    {
        gameCamera = Camera.main;

        foldersSharePosition =
            Vector2.Distance(folderOpen.bounds.center, folderClosed.bounds.center) <
            Mathf.Max(0.15f, folderOpen.bounds.size.magnitude * 0.1f);

        paperStart = folderOpen.bounds.center;
        paperEnd = folderClosed.bounds.center;

        int paperSortingOrder =
            Mathf.Max(folderOpen.sortingOrder, folderClosed.sortingOrder) + 3;

        paperFile.sortingLayerID = folderOpen.sortingLayerID;
        paperFile.sortingOrder = paperSortingOrder;

        secondPaper = Instantiate(
            paperFile,
            paperFile.transform.parent);
        secondPaper.name = "paper_file_second";
        secondPaper.sortingOrder = paperSortingOrder + 1;

        progressBasePosition = progressBarFull.transform.position;
        progressBaseScale = progressBarFull.transform.localScale;
        paperRestScale = paperFile.transform.localScale;
        paperRestRotation = paperFile.transform.rotation;
        folderOpenRestScale = folderOpen.transform.localScale;
        folderClosedRestScale = folderClosed.transform.localScale;
        progressEmptyRestScale = progressBarEmpty.transform.localScale;
        uploadButtonRestScale = uploadButton.transform.localScale;
        progressWidth =
            progressBarFull.sprite.bounds.size.x *
            Mathf.Abs(progressBaseScale.x);

        ResetUploadVisuals();
        StartCoroutine(AnimateEntrance());
    }

    private void Update()
    {
        if (MiniGamePresentationSession.IsInputBlocked)
            return;

        if (!uploading &&
            !completionRequested &&
            inputReady &&
            WasPointerPressed(out Vector2 pointerPosition) &&
            IsPointerOverUploadButton(pointerPosition))
        {
            BeginUpload();
        }

        if (!uploading)
        {
            return;
        }

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / uploadDuration);

        UpdateProgressBar(progress);
        UpdatePaper(paperFile, elapsed / paperCycleDuration, 0.18f);
        UpdatePaper(secondPaper, elapsed / paperCycleDuration + 0.5f, -0.18f);
        UpdateProgressMilestone(progress);

        if (progress >= 1f)
        {
            CompleteUpload();
        }
    }

    public void Configure(
        SpriteRenderer newFolderOpen,
        SpriteRenderer newFolderClosed,
        SpriteRenderer newPaperFile,
        SpriteRenderer newProgressBarEmpty,
        SpriteRenderer newProgressBarFull,
        SpriteRenderer newUploadButton)
    {
        folderOpen = newFolderOpen;
        folderClosed = newFolderClosed;
        paperFile = newPaperFile;
        progressBarEmpty = newProgressBarEmpty;
        progressBarFull = newProgressBarFull;
        uploadButton = newUploadButton;
    }

    private void BeginUpload()
    {
        elapsed = 0f;
        uploading = true;
        lastProgressStep = 0;

        folderOpen.enabled = true;
        folderClosed.enabled = !foldersSharePosition;
        paperFile.enabled = true;
        secondPaper.enabled = true;
        progressBarFull.enabled = true;

        UpdateProgressBar(0f);
        StartCoroutine(MiniGameJuice.PunchScale(
            uploadButton.transform,
            uploadButtonRestScale,
            0.2f,
            0.24f));
        StartCoroutine(MiniGameJuice.FlashColor(
            uploadButton,
            new Color(0.35f, 0.85f, 1f),
            0.3f,
            2));
        StartCoroutine(MiniGameJuice.PunchScale(
            folderOpen.transform,
            folderOpenRestScale,
            0.12f,
            0.28f));
        Debug.Log("File upload started.");
    }

    private void CompleteUpload()
    {
        if (completionRequested)
        {
            return;
        }

        completionRequested = true;
        uploading = false;
        UpdateProgressBar(1f);

        paperFile.enabled = false;
        secondPaper.enabled = false;

        if (foldersSharePosition)
        {
            folderOpen.enabled = false;
            folderClosed.enabled = true;
        }

        Debug.Log("File upload completed.");

        StartCoroutine(AnimateUploadComplete());
    }

    private IEnumerator AnimateUploadComplete()
    {
        SpriteRenderer completedFolder = folderClosed.enabled
            ? folderClosed
            : folderOpen;
        Vector3 completedFolderScale = completedFolder == folderClosed
            ? folderClosedRestScale
            : folderOpenRestScale;

        StartCoroutine(MiniGameJuice.PunchScale(
            completedFolder.transform,
            completedFolderScale,
            0.2f,
            0.36f));
        StartCoroutine(MiniGameJuice.FlashColor(
            completedFolder,
            new Color(0.55f, 1f, 0.62f),
            0.42f,
            2));
        StartCoroutine(MiniGameJuice.PunchScale(
            progressBarFull.transform,
            progressBaseScale,
            0.12f,
            0.32f));
        StartCoroutine(MiniGameJuice.FlashColor(
            progressBarFull,
            new Color(0.45f, 1f, 0.72f),
            0.42f,
            2));

        TaskMissionSession.CompleteLaunchedTaskForScene(
            SceneManager.GetActiveScene().name);

        yield return new WaitForSecondsRealtime(0.58f);

        if (!string.IsNullOrWhiteSpace(completionSceneName))
        {
            SceneManager.LoadScene(completionSceneName);
        }
    }

    private void ResetUploadVisuals()
    {
        uploading = false;
        completionRequested = false;
        inputReady = false;
        elapsed = 0f;
        lastProgressStep = 0;

        paperFile.enabled = false;
        secondPaper.enabled = false;
        progressBarFull.enabled = false;

        folderOpen.enabled = true;
        folderClosed.enabled = !foldersSharePosition;
        UpdateProgressBar(0f);
        ResetPaper(paperFile);
        ResetPaper(secondPaper);
    }

    private IEnumerator AnimateEntrance()
    {
        StartCoroutine(MiniGameJuice.PopIn(
            progressBarEmpty.transform,
            progressEmptyRestScale,
            0.28f,
            1.16f));
        StartCoroutine(MiniGameJuice.PopIn(
            uploadButton.transform,
            uploadButtonRestScale,
            0.25f,
            1.18f));

        if (folderClosed.enabled)
        {
            StartCoroutine(MiniGameJuice.PopIn(
                folderClosed.transform,
                folderClosedRestScale,
                0.3f,
                1.14f));
        }

        yield return MiniGameJuice.PopIn(
            folderOpen.transform,
            folderOpenRestScale,
            0.32f,
            1.18f);
        inputReady = true;
    }

    private void UpdatePaper(
        SpriteRenderer paper,
        float phase,
        float verticalOffset)
    {
        float t = Mathf.Repeat(phase, 1f);
        float easedT = Mathf.SmoothStep(0f, 1f, t);

        Vector3 position = Vector3.Lerp(paperStart, paperEnd, easedT);
        position.y += verticalOffset +
            Mathf.Sin(t * Mathf.PI) * 0.32f;
        position.z = paperFile.transform.position.z;

        paper.transform.position = position;
        paper.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Lerp(-9f, 9f, easedT));
        paper.transform.localScale = paperRestScale *
            (0.9f + Mathf.Sin(t * Mathf.PI) * 0.16f);

        float fadeIn = Mathf.Clamp01(t / 0.08f);
        float fadeOut = Mathf.Clamp01((0.9f - t) / 0.1f);
        Color paperColor = Color.white;
        paperColor.a = Mathf.Min(fadeIn, fadeOut);
        paper.color = paperColor;
        paper.enabled = t < 0.9f;
    }

    private void UpdateProgressMilestone(float progress)
    {
        int progressStep = Mathf.Min(4, Mathf.FloorToInt(progress * 4f));
        if (progressStep <= lastProgressStep)
        {
            return;
        }

        lastProgressStep = progressStep;
        StartCoroutine(MiniGameJuice.PunchScale(
            progressBarEmpty.transform,
            progressEmptyRestScale,
            0.06f,
            0.2f));
        StartCoroutine(MiniGameJuice.FlashColor(
            progressBarEmpty,
            new Color(0.35f, 0.82f, 1f),
            0.2f,
            1));
    }

    private void ResetPaper(SpriteRenderer paper)
    {
        if (paper == null)
        {
            return;
        }

        paper.transform.localScale = paperRestScale;
        paper.transform.rotation = paperRestRotation;
        paper.color = Color.white;
    }

    private void UpdateProgressBar(float progress)
    {
        Vector3 scale = progressBaseScale;
        scale.x = progressBaseScale.x * progress;
        progressBarFull.transform.localScale = scale;

        float direction = Mathf.Sign(progressBaseScale.x);
        Vector3 right = progressBarFull.transform.right * direction;

        progressBarFull.transform.position =
            progressBasePosition -
            right * (progressWidth * (1f - progress) * 0.5f);
    }

    private bool IsPointerOverUploadButton(Vector2 screenPosition)
    {
        if (gameCamera == null)
        {
            return false;
        }

        float cameraDistance = Mathf.Abs(
            gameCamera.transform.position.z -
            uploadButton.transform.position.z);

        Vector3 worldPosition = gameCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                cameraDistance));

        return uploadButton.bounds.Contains(
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                uploadButton.bounds.center.z));
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
