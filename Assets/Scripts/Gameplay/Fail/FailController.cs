using UnityEngine;
using Zenject;

public class FailController : IFailController
{
    private readonly SignalBus _signalBus;
    private readonly GameFlowModel _stateModel;
    private readonly IGameFlowController _gameFlow;

    public FailController(SignalBus signalBus, GameFlowModel stateModel, [Inject(Optional = true)] IGameFlowController gameFlow)
    {
        _signalBus = signalBus;
        _stateModel = stateModel;
        _gameFlow = gameFlow;
    }

    public void TryFail(ResultReason reason, string detail = null)
    {
        if (!_stateModel.CanResolve)
            return;

        if (_gameFlow != null)
        {
            _gameFlow.SetLose(detail ?? reason.ToString());
        }
        else
        {
            _signalBus.Fire(new LoseSignal(reason, detail));
        }
    }
}
