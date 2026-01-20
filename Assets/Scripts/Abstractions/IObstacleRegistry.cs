using System;

public interface IObstacleRegistry
{
    event Action PathCleared;
    bool IsPathCleared { get; }
    bool HasRegisteredObstacles { get; }
    bool AllowEmptyPathStart { get; }
    void Register(IObstacle obstacle);
    void NotifyObstacleCleared(IObstacle obstacle);
    void MarkPathCleared();
}
