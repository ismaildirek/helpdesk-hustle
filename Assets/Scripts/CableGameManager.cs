using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class CableGameManager : MonoBehaviour
{
    [SerializeField] private int totalCables = 6;
    [SerializeField] private UnityEvent onCompleted = new();
    [SerializeField] private string completionSceneName = "YeniOfis";
    [SerializeField, Min(0f)] private float completionDelay = 0.85f;

    private int connectedCables;
    private bool completionRequested;

    public bool IsComplete => connectedCables >= totalCables;
    public int ConnectedCables => connectedCables;

    private void Awake()
    {
        connectedCables = 0;
        completionRequested = false;
    }

    public void RegisterConnection()
    {
        if (IsComplete)
        {
            return;
        }

        connectedCables = Mathf.Min(connectedCables + 1, totalCables);

        if (IsComplete)
        {
            completionRequested = true;
            ProceduralGameAudio.Play(GameSound.TaskCompleted);
            TaskMissionSession.CompleteLaunchedTaskForScene(
                SceneManager.GetActiveScene().name);
            onCompleted?.Invoke();

            if (!string.IsNullOrWhiteSpace(completionSceneName))
            {
                StartCoroutine(LoadCompletionSceneAfterDelay());
            }
        }
    }

    public void Configure(int cableCount)
    {
        totalCables = cableCount;
    }

    private IEnumerator LoadCompletionSceneAfterDelay()
    {
        if (completionDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(completionDelay);
        }

        if (completionRequested)
        {
            SceneManager.LoadScene(completionSceneName);
        }
    }
}
