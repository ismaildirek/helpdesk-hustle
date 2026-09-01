using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TaskCardMissionBoard : MonoBehaviour
{
    [Serializable]
    public sealed class TaskDefinition
    {
        public string id;
        public Sprite icon;
        [TextArea(2, 3)] public string description;
        public string location;
        [HideInInspector] public float duration = 30f;
    }

    [Serializable]
    public sealed class TaskSlotView
    {
        public SpriteRenderer iconRenderer;
        public TextMesh descriptionText;
        public TextMesh locationText;
        public TextMesh timerText;

        [NonSerialized] public int taskIndex = -1;
        [NonSerialized] public float remainingTime;
        [NonSerialized] public int displayedSeconds = -1;
    }

    [Header("Task Pool")]
    [SerializeField] private TaskDefinition[] tasks;

    [Header("Card Slots")]
    [SerializeField] private TaskSlotView[] slots;
    [SerializeField, Min(0.1f)] private float iconMaximumSize = 2.15f;

    [Header("Task Metadata Readability")]
    [SerializeField] private Color locationTextColor =
        new Color32(4, 27, 45, 255);
    [SerializeField, Min(0.01f)] private float descriptionCharacterSize = 0.095f;
    [SerializeField, Min(0.01f)] private float locationCharacterSize = 0.088f;
    [SerializeField, Min(0.01f)] private float timerCharacterSize = 0.095f;
    [SerializeField] private float descriptionHorizontalOffset = -4.85f;
    [SerializeField] private float descriptionVerticalOffset = 0.02f;
    [SerializeField] private float locationHorizontalOffset = 6.7f;
    [SerializeField] private float locationVerticalOffset = 0.85f;
    [SerializeField] private float timerHorizontalOffset = 6.7f;
    [SerializeField] private float timerVerticalOffset = -0.85f;

    [Header("Task Timing")]
    [SerializeField, Min(1f)] private float minimumTaskDuration = 30f;
    [SerializeField, Min(1f)] private float maximumTaskDuration = 50f;

    [Header("Timer Colours")]
    [SerializeField] private Color normalTimerColor =
        new Color32(58, 11, 70, 255);
    [SerializeField] private Color urgentTimerColor =
        new Color32(125, 0, 22, 255);
    [SerializeField, Min(1f)] private float urgentThreshold = 10f;

    private bool initialized;
    private int nextTaskCursor;
    private TaskCardToggle taskCardToggle;

    private void Awake()
    {
        taskCardToggle = GetComponent<TaskCardToggle>();
        ConfigureMetadataReadability();
    }

    private void Start()
    {
        if (!BossIntroDialogue.IsBlockingOfficeInput)
            InitializeBoard();
    }

    private void ConfigureMetadataReadability()
    {
        if (slots == null)
            return;

        foreach (TaskSlotView slot in slots)
        {
            if (slot == null)
                continue;

            if (slot.descriptionText != null)
            {
                slot.descriptionText.characterSize = descriptionCharacterSize;
                slot.descriptionText.fontStyle = FontStyle.Bold;
                slot.descriptionText.lineSpacing = 0.78f;
                SetLocalPosition(
                    slot.descriptionText.transform,
                    descriptionHorizontalOffset,
                    descriptionVerticalOffset);
            }

            if (slot.locationText != null)
            {
                slot.locationText.characterSize = locationCharacterSize;
                slot.locationText.fontStyle = FontStyle.Bold;
                slot.locationText.color = locationTextColor;
                SetLocalPosition(
                    slot.locationText.transform,
                    locationHorizontalOffset,
                    locationVerticalOffset);
            }

            if (slot.timerText != null)
            {
                slot.timerText.characterSize = timerCharacterSize;
                slot.timerText.fontStyle = FontStyle.Bold;
                SetLocalPosition(
                    slot.timerText.transform,
                    timerHorizontalOffset,
                    timerVerticalOffset);
            }
        }
    }

    private static void SetLocalPosition(
        Transform target,
        float horizontalPosition,
        float verticalPosition)
    {
        Vector3 position = target.localPosition;
        position.x = horizontalPosition;
        position.y = verticalPosition;
        target.localPosition = position;
    }

    private void Update()
    {
        if (BossIntroDialogue.IsBlockingOfficeInput ||
            BossAngerSession.HasLost ||
            GamePauseSession.IsPaused)
            return;

        if (!initialized)
            InitializeBoard();

        if (!initialized)
            return;

        for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
        {
            TaskSlotView slot = slots[slotIndex];
            if (slot == null || slot.taskIndex < 0)
                continue;

            slot.remainingTime -= Time.unscaledDeltaTime;
            if (slot.remainingTime <= 0f)
            {
                TaskMissionSession.ExpireTask(slotIndex);
                if (BossAngerSession.HasLost)
                    return;

                AssignNextTask(slotIndex);
                continue;
            }

            UpdateTimer(slot);
        }
    }

    private void InitializeBoard()
    {
        if (tasks == null || tasks.Length == 0 ||
            slots == null || slots.Length == 0 ||
            !HasPlayableTask())
        {
            Debug.LogWarning(
                "Task card mission board has no playable tasks or slots.",
                this);
            return;
        }

        TaskMissionSession.EnsureSlotCount(slots.Length);
        nextTaskCursor = UnityEngine.Random.Range(0, tasks.Length);
        bool[] slotsNeedingTask = new bool[slots.Length];

        for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
        {
            if (BossAngerSession.HasLost)
                return;

            if (TaskMissionSession.TryGetTask(
                    slotIndex,
                    out TaskMissionSnapshot savedTask) &&
                TryFindTaskIndex(
                    savedTask.TaskId,
                    out int taskIndex))
            {
                ApplyTaskToSlot(
                    slotIndex,
                    taskIndex,
                    savedTask.RemainingTime,
                    new TaskMissionRoute(
                        savedTask.SceneName,
                        savedTask.Floor,
                        savedTask.Room));
                nextTaskCursor =
                    (taskIndex + 1) % tasks.Length;
            }
            else
            {
                slotsNeedingTask[slotIndex] = true;
            }
        }

        for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
        {
            if (BossAngerSession.HasLost)
                return;

            if (!slotsNeedingTask[slotIndex])
                continue;

            if (TaskMissionSession.TryConsumeCompletedTaskId(
                    slotIndex,
                    out string completedTaskId) &&
                TryFindTaskIndex(
                    completedTaskId,
                    out int completedTaskIndex))
            {
                StartCoroutine(ShowCompletedThenAssign(
                    slotIndex,
                    completedTaskIndex,
                    completedTaskId));
            }
            else
            {
                AssignNextTask(slotIndex);
            }
        }

        initialized = true;
    }

    private IEnumerator ShowCompletedThenAssign(
        int slotIndex,
        int completedTaskIndex,
        string completedTaskId)
    {
        TaskSlotView slot = slots[slotIndex];
        if (slot == null)
            yield break;

        TaskMissionRouteCatalog.TryGetRoute(
            completedTaskId,
            out TaskMissionRoute route);
        ApplyTaskToSlot(
            slotIndex,
            completedTaskIndex,
            0f,
            route);
        slot.taskIndex = -1;
        if (slot.timerText != null)
            slot.timerText.text = string.Empty;

        TextMesh stamp = CreateCompletionStamp(slot);
        taskCardToggle?.ShowCompletionFeedback(1.35f);

        if (stamp != null)
        {
            Vector3 restingScale = stamp.transform.localScale;
            yield return MiniGameJuice.PopIn(
                stamp.transform,
                restingScale,
                0.28f,
                1.24f);
            yield return new WaitForSecondsRealtime(0.62f);

            Color color = stamp.color;
            float elapsed = 0f;
            const float fadeDuration = 0.24f;
            while (elapsed < fadeDuration && stamp != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeDuration);
                color.a = 1f - progress;
                stamp.color = color;
                stamp.transform.localScale = restingScale *
                    Mathf.Lerp(1f, 1.12f, progress);
                yield return null;
            }

            if (stamp != null)
                Destroy(stamp.gameObject);
        }

        AssignNextTask(slotIndex, completedTaskId);
    }

    private static TextMesh CreateCompletionStamp(TaskSlotView slot)
    {
        Transform parent = slot.descriptionText != null
            ? slot.descriptionText.transform.parent
            : slot.iconRenderer != null
                ? slot.iconRenderer.transform.parent
                : null;
        Font font = slot.descriptionText != null
            ? slot.descriptionText.font
            : null;
        if (parent == null || font == null)
            return null;

        GameObject stampObject = new("CompletedStamp");
        stampObject.transform.SetParent(parent, false);
        stampObject.transform.localPosition = new Vector3(1.25f, 0f, -0.3f);
        stampObject.transform.localRotation = Quaternion.Euler(0f, 0f, -6f);
        stampObject.transform.localScale = Vector3.one;

        TextMesh stamp = stampObject.AddComponent<TextMesh>();
        stamp.text = "COMPLETED";
        stamp.font = font;
        stamp.fontSize = 82;
        stamp.characterSize = 0.115f;
        stamp.anchor = TextAnchor.MiddleCenter;
        stamp.alignment = TextAlignment.Center;
        stamp.fontStyle = FontStyle.Bold;
        stamp.color = new Color32(56, 224, 104, 255);

        MeshRenderer renderer = stampObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.sortingOrder = 32;
        return stamp;
    }

    private void AssignNextTask(
        int slotIndex,
        string excludedTaskId = null)
    {
        TaskSlotView slot = slots[slotIndex];
        if (slot == null)
            return;

        int selectedIndex = FindNextAvailableTask(
            slotIndex,
            excludedTaskId);
        if (selectedIndex < 0)
        {
            ClearSlot(slotIndex);
            return;
        }

        TaskDefinition task = tasks[selectedIndex];
        if (!TaskMissionRouteCatalog.TryCreateAssignmentRoute(
                task.id,
                out TaskMissionRoute route))
        {
            ClearSlot(slotIndex);
            return;
        }

        float duration = GetTaskDuration(task);
        ApplyTaskToSlot(slotIndex, selectedIndex, duration, route);
        TaskMissionSession.AssignTask(
            slotIndex,
            task.id,
            route,
            duration);
        nextTaskCursor = (selectedIndex + 1) % tasks.Length;
    }

    private void ApplyTaskToSlot(
        int slotIndex,
        int taskIndex,
        float remainingTime,
        TaskMissionRoute route)
    {
        TaskSlotView slot = slots[slotIndex];
        TaskDefinition task = tasks[taskIndex];

        slot.taskIndex = taskIndex;
        slot.remainingTime = Mathf.Max(0f, remainingTime);
        slot.displayedSeconds = -1;

        if (slot.iconRenderer != null)
        {
            slot.iconRenderer.sprite = task.icon;
            slot.iconRenderer.enabled = task.icon != null;
            FitIcon(slot.iconRenderer);
        }

        if (slot.descriptionText != null)
            slot.descriptionText.text = NormalizeDescription(task.description);

        if (slot.locationText != null)
        {
            slot.locationText.text =
                $"FLOOR {route.Floor} | ROOM {route.Room}";
        }

        UpdateTimer(slot);
    }

    public static string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        string[] lines = description
            .Replace("\r", string.Empty)
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int index = 0; index < lines.Length; index++)
            lines[index] = lines[index].Trim();

        return string.Join("\n", lines);
    }

    private int FindNextAvailableTask(
        int slotIndex,
        string excludedTaskId)
    {
        for (int offset = 0; offset < tasks.Length; offset++)
        {
            int candidate = (nextTaskCursor + offset) % tasks.Length;
            TaskDefinition candidateTask = tasks[candidate];
            if (candidateTask == null ||
                candidateTask.id == excludedTaskId ||
                !TaskMissionRouteCatalog.TryGetRoute(
                    candidateTask.id,
                    out _))
            {
                continue;
            }

            bool alreadyVisible = false;

            for (int otherIndex = 0;
                 otherIndex < slots.Length;
                 otherIndex++)
            {
                if (otherIndex != slotIndex &&
                    slots[otherIndex] != null &&
                    slots[otherIndex].taskIndex == candidate)
                {
                    alreadyVisible = true;
                    break;
                }
            }

            if (!alreadyVisible)
                return candidate;
        }

        for (int taskIndex = 0;
             taskIndex < tasks.Length;
             taskIndex++)
        {
            if (tasks[taskIndex] != null &&
                tasks[taskIndex].id != excludedTaskId &&
                TaskMissionRouteCatalog.TryGetRoute(
                    tasks[taskIndex].id,
                    out _))
            {
                return taskIndex;
            }
        }

        return -1;
    }

    private bool HasPlayableTask()
    {
        for (int taskIndex = 0;
             taskIndex < tasks.Length;
             taskIndex++)
        {
            if (tasks[taskIndex] != null &&
                TaskMissionRouteCatalog.TryGetRoute(
                    tasks[taskIndex].id,
                    out _))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindTaskIndex(
        string taskId,
        out int taskIndex)
    {
        for (int index = 0; index < tasks.Length; index++)
        {
            if (tasks[index] != null &&
                tasks[index].id == taskId &&
                TaskMissionRouteCatalog.TryGetRoute(
                    tasks[index].id,
                    out _))
            {
                taskIndex = index;
                return true;
            }
        }

        taskIndex = -1;
        return false;
    }

    private void ClearSlot(int slotIndex)
    {
        TaskSlotView slot = slots[slotIndex];
        if (slot == null)
            return;

        slot.taskIndex = -1;
        slot.remainingTime = 0f;
        slot.displayedSeconds = -1;
        TaskMissionSession.ClearTask(slotIndex);

        if (slot.iconRenderer != null)
        {
            slot.iconRenderer.sprite = null;
            slot.iconRenderer.enabled = false;
        }

        if (slot.descriptionText != null)
            slot.descriptionText.text = string.Empty;

        if (slot.locationText != null)
            slot.locationText.text = string.Empty;

        if (slot.timerText != null)
            slot.timerText.text = string.Empty;
    }

    private void FitIcon(SpriteRenderer iconRenderer)
    {
        Sprite sprite = iconRenderer.sprite;
        if (sprite == null)
            return;

        Vector2 size = sprite.bounds.size;
        float largestSide = Mathf.Max(size.x, size.y);
        float scale = largestSide > 0f
            ? iconMaximumSize / largestSide
            : 1f;

        iconRenderer.transform.localScale =
            new Vector3(scale, scale, 1f);
    }

    private void UpdateTimer(TaskSlotView slot)
    {
        if (slot.timerText == null)
            return;

        int totalSeconds = Mathf.Max(
            0,
            Mathf.CeilToInt(slot.remainingTime));
        if (slot.displayedSeconds == totalSeconds)
            return;

        slot.displayedSeconds = totalSeconds;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        slot.timerText.text = $"TIME  {minutes:00}:{seconds:00}";
        slot.timerText.color = totalSeconds <= urgentThreshold
            ? urgentTimerColor
            : normalTimerColor;
    }

    private float GetTaskDuration(TaskDefinition task)
    {
        float baseDuration;
        if (task != null && task.duration > 0f)
        {
            baseDuration = task.duration;
        }
        else
        {
            int minimum = Mathf.Max(
                1,
                Mathf.CeilToInt(minimumTaskDuration));
            int maximum = Mathf.Max(
                minimum,
                Mathf.FloorToInt(maximumTaskDuration));

            baseDuration = UnityEngine.Random.Range(
                minimum,
                maximum + 1);
        }

        return Mathf.Max(
            12f,
            baseDuration * GameProgressionSession.TaskDurationMultiplier);
    }

#if UNITY_EDITOR
    public void ConfigureEditor(
        TaskSlotView[] configuredSlots,
        TaskDefinition[] defaultTasks)
    {
        slots = configuredSlots;
        // This board is generated from the editor catalogue. Refreshing the
        // complete catalogue also adds newly introduced playable tasks to
        // existing scenes instead of keeping an older serialized task array.
        tasks = defaultTasks;
    }
#endif
}
