using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

public class WinScreenView : ResultScreenView
{
    [SerializeField] private Button continueButton;

    private void OnEnable()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(ReloadLevel);
    }

    private void OnDisable()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(ReloadLevel);
    }

    private static void ReloadLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
