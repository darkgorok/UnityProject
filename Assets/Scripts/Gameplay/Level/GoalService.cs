using System;
using Zenject;

public sealed class GoalService : IInitializable, IDisposable
{
    private readonly SignalBus _signalBus;
    private readonly PathState _pathState;
    private readonly IObstacleRegistry _obstacleRegistry;

    public GoalService(SignalBus signalBus, PathState pathState, IObstacleRegistry obstacleRegistry)
    {
        _signalBus = signalBus;
        _pathState = pathState;
        _obstacleRegistry = obstacleRegistry;
    }

    public void Initialize()
    {
        _pathState.Cleared += HandlePathCleared;

        _signalBus.Fire(new GameStartSignal());
        if (_obstacleRegistry.AllowEmptyPathStart && _obstacleRegistry.IsPathCleared)
            _signalBus.Fire(new PathClearedSignal());
    }

    public void Dispose()
    {
        _pathState.Cleared -= HandlePathCleared;
    }

    private void HandlePathCleared()
    {
        _signalBus.Fire(new PathClearedSignal());
    }
}
