using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameLauncher : MonoBehaviour
{
    [SerializeField] private NoTaskRoomFeedback noTaskFeedback;

    private FloorRoomPresentation roomPresentation;

    private void Awake()
    {
        if (noTaskFeedback == null)
        {
            noTaskFeedback =
                FindFirstObjectByType<NoTaskRoomFeedback>(
                    FindObjectsInactive.Include);
        }

        roomPresentation =
            FindFirstObjectByType<FloorRoomPresentation>(
                FindObjectsInactive.Include);
    }

    public void OpenCableGame()
    {
        OpenAssignedTask();
    }

    public void OpenFileUpload()
    {
        OpenAssignedTask();
    }

    public void OpenVirusGame()
    {
        OpenAssignedTask();
    }

    public void OpenBrokenPcRepair()
    {
        OpenAssignedTask();
    }

    public void OpenBrokenMonitorRepair()
    {
        OpenAssignedTask();
    }

    public void OpenModemGame()
    {
        OpenAssignedTask();
    }

    public void OpenAssignedTask()
    {
        if (GamePauseSession.IsPaused)
            return;

        ProceduralGameAudio.Play(GameSound.UiClick, 0.025f);

        if (roomPresentation != null)
        {
            roomPresentation.SelectRoom(gameObject.name);
            return;
        }

        if (noTaskFeedback != null &&
            !noTaskFeedback.CanAcceptRoomSelection)
        {
            return;
        }

        if (TaskMissionSession.TryGetSceneForRoomButton(
                gameObject.name,
                out string sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        noTaskFeedback?.ShowRandomFeedback();
    }

#if UNITY_EDITOR
    public void ConfigureNoTaskFeedback(
        NoTaskRoomFeedback configuredFeedback)
    {
        noTaskFeedback = configuredFeedback;
    }
#endif
}
