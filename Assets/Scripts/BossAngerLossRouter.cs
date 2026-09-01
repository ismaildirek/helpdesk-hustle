using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class BossAngerLossRouter : MonoBehaviour
{
    private const string EntranceSceneName = "Giris_Ekran";

    private static BossAngerLossRouter instance;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        instance = null;
    }

    public static void ScheduleReturnToEntrance()
    {
        if (instance != null)
            return;

        GameObject routerObject =
            new("BossAngerLossRouter");
        DontDestroyOnLoad(routerObject);
        instance = routerObject.AddComponent<BossAngerLossRouter>();
    }

    private IEnumerator Start()
    {
        // Let the failure-causing interaction finish its current scene call.
        yield return null;

        if (BossAngerSession.HasLost &&
            SceneManager.GetActiveScene().name != EntranceSceneName)
        {
            SceneManager.LoadScene(EntranceSceneName);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
