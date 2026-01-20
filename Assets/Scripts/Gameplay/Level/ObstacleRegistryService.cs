using System.Collections.Generic;
using UnityEngine;

public sealed class ObstacleRegistryService : IObstacleRegistry, IObstacleResolver
{
    private readonly HashSet<IObstacle> _obstacles = new HashSet<IObstacle>();
    private readonly Dictionary<Collider, IObstacle> _lookup = new Dictionary<Collider, IObstacle>();
    private readonly PathState _pathState;
    private readonly bool _allowEmptyPathStart;

    public ObstacleRegistryService(PathState pathState, LevelFlowConfig config = null)
    {
        _pathState = pathState;
        _allowEmptyPathStart = config != null && config.allowEmptyPathStart;
    }

    public bool IsPathCleared => _pathState.IsCleared || (!_pathState.HasRegisteredObstacles && _allowEmptyPathStart);
    public bool HasRegisteredObstacles => _pathState.HasRegisteredObstacles;
    public bool AllowEmptyPathStart => _allowEmptyPathStart;

    public void Register(IObstacle obstacle)
    {
        if (obstacle == null)
            return;

        if (_obstacles.Add(obstacle))
            _pathState.RegisterObstacle();
    }

    public void NotifyObstacleCleared(IObstacle obstacle)
    {
        if (obstacle == null)
            return;

        if (_obstacles.Remove(obstacle))
            _pathState.NotifyObstacleCleared();
    }

    public void Register(Collider collider, IObstacle obstacle)
    {
        if (collider == null || obstacle == null)
            return;

        _lookup[collider] = obstacle;
    }

    public void Unregister(Collider collider)
    {
        if (collider == null)
            return;

        _lookup.Remove(collider);
    }

    public bool TryGetObstacle(Collider collider, out IObstacle obstacle)
    {
        if (collider == null)
        {
            obstacle = null;
            return false;
        }

        return _lookup.TryGetValue(collider, out obstacle);
    }
}
