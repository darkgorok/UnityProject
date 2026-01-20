using UnityEngine;
using Zenject;

public class GameUiController : MonoBehaviour
{
    [Inject(Optional = true, Id = "Win")] private IResultScreen _winScreen;
    [Inject(Optional = true, Id = "Lose")] private IResultScreen _loseScreen;
    [Inject(Optional = true, Id = "UiRoot")] private Transform _uiRoot;

    private void OnEnable()
    {
        TryAttachToUiRoot(_winScreen);
        TryAttachToUiRoot(_loseScreen);

        _winScreen?.Hide();
        _loseScreen?.Hide();
    }

    private void TryAttachToUiRoot(IResultScreen screen)
    {
        if (_uiRoot == null || screen == null)
            return;

        if (screen is Component component && component.transform.parent != _uiRoot)
            component.transform.SetParent(_uiRoot, false);
    }
}
