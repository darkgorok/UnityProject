public class GameFlowModel
{
    public GameFlowState State { get; private set; } = GameFlowState.Init;
    public string LastLoseReason { get; private set; }

    public bool CanStart => State == GameFlowState.Init;
    public bool CanResolve => State == GameFlowState.Play;

    public void SetPlay()
    {
        State = GameFlowState.Play;
    }

    public void SetWin()
    {
        State = GameFlowState.Win;
    }

    public void SetLose(string reason)
    {
        LastLoseReason = reason;
        State = GameFlowState.Lose;
    }
}
