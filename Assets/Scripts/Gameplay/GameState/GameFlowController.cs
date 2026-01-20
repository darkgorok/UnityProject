using System;
using Zenject;

public class GameFlowController : IGameFlowController, IInitializable, IDisposable
{
    public event Action<GameFlowState> StateChanged;

    public GameFlowState State => _stateModel.State;
    public string LastLoseReason => _stateModel.LastLoseReason;

    private readonly SignalBus _signalBus;
    private readonly GameFlowModel _stateModel;

    public GameFlowController(SignalBus signalBus, GameFlowModel stateModel)
    {
        _signalBus = signalBus;
        _stateModel = stateModel;
        BeginPlay();
    }

    public void Initialize()
    {
        _signalBus.Subscribe<GameStartSignal>(HandleGameStart);
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<GameStartSignal>(HandleGameStart);
    }

    public void BeginPlay()
    {
        if (!_stateModel.CanStart)
            return;

        _stateModel.SetPlay();
        StateChanged?.Invoke(_stateModel.State);
    }

    public void SetWin()
    {
        if (!_stateModel.CanResolve)
            return;

        _stateModel.SetWin();
        StateChanged?.Invoke(_stateModel.State);
    }

    public void SetLose(string reason)
    {
        if (!_stateModel.CanResolve)
            return;

        _stateModel.SetLose(reason);
        StateChanged?.Invoke(_stateModel.State);
    }

    private void HandleGameStart()
    {
        BeginPlay();
    }
}
