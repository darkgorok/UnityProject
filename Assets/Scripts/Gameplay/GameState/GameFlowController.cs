using System;

public class GameFlowController : IGameFlowController
{
    public event Action<GameFlowState> StateChanged;

    public GameFlowState State => _stateModel.State;
    public string LastLoseReason => _stateModel.LastLoseReason;

    private readonly GameFlowModel _stateModel;

    public GameFlowController(GameFlowModel stateModel)
    {
        _stateModel = stateModel;
        BeginPlay();
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

}
