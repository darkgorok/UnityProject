using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

public class LoseScreenView : ResultScreenView
{
    [SerializeField] private Button retryButton;

    private void OnEnable()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(ReloadLevel);
    }

    private void OnDisable()
    {
        if (retryButton != null)
            retryButton.onClick.RemoveListener(ReloadLevel);
    }

    private static void ReloadLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
