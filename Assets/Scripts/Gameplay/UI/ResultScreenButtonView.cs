using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ResultScreenButtonView : ResultScreenView
{
    [SerializeField] private Button actionButton;
    [Inject(Optional = true)] private ILevelReloader _reloader;

    private void OnEnable()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(ReloadLevel);
    }

    private void OnDisable()
    {
        if (actionButton != null)
            actionButton.onClick.RemoveListener(ReloadLevel);
    }

    private void ReloadLevel()
    {
        _reloader?.ReloadCurrent();
    }
}
