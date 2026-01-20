using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class WinScreenView : ResultScreenView
{
    [SerializeField] private Button continueButton;
    [Inject(Optional = true)] private ILevelReloader _reloader;

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

    private void ReloadLevel()
    {
        _reloader?.ReloadCurrent();
    }
}
