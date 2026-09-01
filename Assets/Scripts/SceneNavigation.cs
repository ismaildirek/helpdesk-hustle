using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigation : MonoBehaviour
{
    public void OpenMunicipality()
    {
        if (BossIntroDialogue.IsBlockingOfficeInput)
            return;

        TaskMissionSession.AbandonLaunchedTaskForScene(
            SceneManager.GetActiveScene().name);
        if (BossAngerSession.HasLost)
            return;

        SceneManager.LoadScene("katlar");
    }

    public void OpenMainOffice()
    {
        if (BossIntroDialogue.IsBlockingOfficeInput)
            return;

        TaskMissionSession.AbandonLaunchedTaskForScene(
            SceneManager.GetActiveScene().name);
        if (BossAngerSession.HasLost)
            return;

        SceneManager.LoadScene("YeniOfis");
    }
}
