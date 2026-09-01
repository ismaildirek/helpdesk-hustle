using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
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

        roomPresentation = GetComponent<FloorRoomPresentation>();
        if (roomPresentation == null)
        {
            roomPresentation =
                FindFirstObjectByType<FloorRoomPresentation>(
                    FindObjectsInactive.Include);
        }
    }

    public void Tiklandi()
    {
        Debug.Log("BUTON ÇALIŞTI");
    }

    private void OpenAssignedTask(string buttonName)
    {
        if (roomPresentation != null)
        {
            roomPresentation.SelectRoom(buttonName);
            return;
        }

        if (noTaskFeedback != null &&
            !noTaskFeedback.CanAcceptRoomSelection)
        {
            return;
        }

        if (TaskMissionSession.TryGetSceneForRoomButton(
                buttonName,
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
