public interface IObstacleRegistry
{
    bool IsPathCleared { get; }
    bool HasRegisteredObstacles { get; }
    bool AllowEmptyPathStart { get; }
    void Register(IObstacle obstacle);
    void NotifyObstacleCleared(IObstacle obstacle);
}
