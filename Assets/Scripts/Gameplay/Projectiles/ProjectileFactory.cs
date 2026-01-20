using UnityEngine;
using Zenject;

public class ProjectileFactory : MonoBehaviour, IProjectileFactory
{
    [SerializeField] private Projectile projectilePrefab;
    [Inject(Optional = true)] private Projectile.Pool _pool;
    [Inject(Optional = true)] private DiContainer _container;

    public Projectile Create(Vector3 position, Quaternion rotation)
    {
        if (_pool != null)
        {
            var pooled = _pool.Spawn();
            pooled.transform.SetPositionAndRotation(position, rotation);
            return pooled;
        }

        if (projectilePrefab == null)
            return null;

        var projectile = Object.Instantiate(projectilePrefab);
        _container?.InjectGameObject(projectile.gameObject);
        projectile.transform.SetPositionAndRotation(position, rotation);
        return projectile;
    }
}
