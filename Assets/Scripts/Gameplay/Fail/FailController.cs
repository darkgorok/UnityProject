public class FailController : IFailController
{
    private readonly GameFlowModel _stateModel;
    private readonly IGameFlowController _gameFlow;

    public FailController(GameFlowModel stateModel, IGameFlowController gameFlow)
    {
        _stateModel = stateModel;
        _gameFlow = gameFlow;
    }

    public void TryFail(ResultReason reason, string detail = null)
    {
        if (!_stateModel.CanResolve)
            return;

        _gameFlow.SetLose(detail ?? reason.ToString());
    }
}
