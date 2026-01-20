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
    public event System.Action PathCleared
    {
        add => _pathState.Cleared += value;
        remove => _pathState.Cleared -= value;
    }

    public void Register(IObstacle obstacle)
    {
        if (!IsValidObstacle(obstacle))
            return;

        if (_obstacles.Add(obstacle))
            _pathState.RegisterObstacle();
    }

    public void NotifyObstacleCleared(IObstacle obstacle)
    {
        if (!IsValidObstacle(obstacle))
            return;

        if (_obstacles.Remove(obstacle))
            _pathState.NotifyObstacleCleared();
    }

    public void MarkPathCleared()
    {
        if (_pathState.IsCleared)
            return;

        _pathState.MarkCleared();
    }

    public void Register(Collider collider, IObstacle obstacle)
    {
        if (!IsValidCollider(collider) || !IsValidObstacle(obstacle))
            return;

        _lookup[collider] = obstacle;
    }

    public void Unregister(Collider collider)
    {
        if (!IsValidCollider(collider))
            return;

        _lookup.Remove(collider);
    }

    public bool TryGetObstacle(Collider collider, out IObstacle obstacle)
    {
        if (!IsValidCollider(collider))
        {
            obstacle = null;
            return false;
        }

        return _lookup.TryGetValue(collider, out obstacle);
    }

    private static bool IsValidObstacle(IObstacle obstacle)
    {
        return obstacle != null;
    }

    private static bool IsValidCollider(Collider collider)
    {
        return collider != null;
    }
}
