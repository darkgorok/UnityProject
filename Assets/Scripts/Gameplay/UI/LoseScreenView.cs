using UnityEngine.UI;
using Zenject;
using UnityEngine;

public class LoseScreenView : ResultScreenView
{
    [SerializeField] private Button retryButton;
    [Inject(Optional = true)] private ILevelReloader _reloader;

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

    private void ReloadLevel()
    {
        _reloader?.ReloadCurrent();
    }
}
