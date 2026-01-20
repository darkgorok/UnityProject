using System;
using UnityEngine;
using Zenject;

public sealed class GameUiPresenter : IInitializable, IDisposable, ITickable
{
    private readonly IGameFlowController _gameFlow;
    private readonly SignalBus _signalBus;
    private readonly IResultScreen _winScreen;
    private readonly IResultScreen _loseScreen;
    private readonly Transform _uiRoot;
    private bool _resultShown;

    public GameUiPresenter(
        IGameFlowController gameFlow,
        SignalBus signalBus,
        [Inject(Id = "Win")] IResultScreen winScreen,
        [Inject(Id = "Lose")] IResultScreen loseScreen,
        [InjectOptional(Id = "UiRoot")] Transform uiRoot)
    {
        _gameFlow = gameFlow;
        _signalBus = signalBus;
        _winScreen = winScreen;
        _loseScreen = loseScreen;
        _uiRoot = uiRoot;
    }

    public void Initialize()
    {
        _resultShown = false;
        TryAttachToUiRoot(_winScreen);
        TryAttachToUiRoot(_loseScreen);

        _winScreen?.Hide();
        _loseScreen?.Hide();

        if (_gameFlow != null)
        {
            _gameFlow.StateChanged += HandleStateChanged;
            if (_gameFlow.State != GameFlowState.Play)
                HandleStateChanged(_gameFlow.State);
        }

        _signalBus.Subscribe<WinSignal>(HandleWinSignal);
        _signalBus.Subscribe<LoseSignal>(HandleLoseSignal);
    }

    public void Tick()
    {
        if (_resultShown || _gameFlow == null)
            return;

        if (_gameFlow.State == GameFlowState.Win)
        {
            ShowWin();
            return;
        }

        if (_gameFlow.State == GameFlowState.Lose)
            ShowLose();
    }

    public void Dispose()
    {
        if (_gameFlow != null)
            _gameFlow.StateChanged -= HandleStateChanged;

        _signalBus.TryUnsubscribe<WinSignal>(HandleWinSignal);
        _signalBus.TryUnsubscribe<LoseSignal>(HandleLoseSignal);
    }

    private void HandleStateChanged(GameFlowState state)
    {
        if (_resultShown)
            return;

        switch (state)
        {
            case GameFlowState.Win:
                ShowWin();
                break;
            case GameFlowState.Lose:
                ShowLose();
                break;
        }
    }

    private void HandleWinSignal()
    {
        ShowWin();
    }

    private void HandleLoseSignal(LoseSignal signal)
    {
        ShowLose();
    }

    private void ShowWin()
    {
        if (_resultShown)
            return;

        _resultShown = true;
        _winScreen?.Show();
    }

    private void ShowLose()
    {
        if (_resultShown)
            return;

        _resultShown = true;
        _loseScreen?.Show();
    }

    private void TryAttachToUiRoot(IResultScreen screen)
    {
        if (_uiRoot == null || screen == null)
            return;

        if (screen is UnityEngine.Component component && component.transform.parent != _uiRoot)
            component.transform.SetParent(_uiRoot, false);
    }
}
