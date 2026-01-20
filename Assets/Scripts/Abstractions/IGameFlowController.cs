using System;

public enum GameFlowState
{
    Init,
    Play,
    Win,
    Lose
}

public interface IGameFlowController
{
    GameFlowState State { get; }
    event Action<GameFlowState> StateChanged;
    void BeginPlay();
    void SetWin();
    void SetLose(string reason);
}
