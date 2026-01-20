using Zenject;

public sealed class GoalService : IInitializable
{
    private readonly IObstacleRegistry _obstacleRegistry;

    public GoalService(IObstacleRegistry obstacleRegistry)
    {
        _obstacleRegistry = obstacleRegistry;
    }

    public void Initialize()
    {
        if (_obstacleRegistry.AllowEmptyPathStart && _obstacleRegistry.IsPathCleared)
            _obstacleRegistry.MarkPathCleared();
    }
}
