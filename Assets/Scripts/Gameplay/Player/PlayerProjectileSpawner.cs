using UnityEngine;
using Zenject;

public class PlayerProjectileSpawner : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [Inject(Optional = true)] private Projectile.Pool _projectilePool;
    [Inject] private DiContainer _container;

    public Projectile Spawn(Vector3 position, Quaternion rotation)
    {
        if (_projectilePool != null)
        {
            var pooled = _projectilePool.Spawn();
            pooled.transform.SetPositionAndRotation(position, rotation);
            return pooled;
        }

        var projectile = Instantiate(projectilePrefab);
        _container.InjectGameObject(projectile.gameObject);
        projectile.transform.SetPositionAndRotation(position, rotation);
        return projectile;
    }
}
