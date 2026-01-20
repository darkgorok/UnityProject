using UnityEngine;

public interface IObstacleResolver
{
    void Register(Collider collider, IObstacle obstacle);
    void Unregister(Collider collider);
    bool TryGetObstacle(Collider collider, out IObstacle obstacle);
}
