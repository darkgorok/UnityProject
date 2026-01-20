using System;

public sealed class PathState
{
    private int _activeObstacleCount;
    private bool _hasRegisteredObstacles;

    public bool IsCleared => _hasRegisteredObstacles && _activeObstacleCount <= 0;
    public bool HasRegisteredObstacles => _hasRegisteredObstacles;

    public event Action Cleared;

    public void RegisterObstacle()
    {
        _hasRegisteredObstacles = true;
        _activeObstacleCount++;
    }

    public void NotifyObstacleCleared()
    {
        if (_activeObstacleCount > 0)
            _activeObstacleCount--;

        if (IsCleared)
            Cleared?.Invoke();
    }
}
