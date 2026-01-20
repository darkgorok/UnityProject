using UnityEngine.SceneManagement;

public sealed class SceneLevelReloader : ILevelReloader
{
    public void ReloadCurrent()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
