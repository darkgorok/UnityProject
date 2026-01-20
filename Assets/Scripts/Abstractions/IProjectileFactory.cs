using UnityEngine;

public interface IProjectileFactory
{
    Projectile Create(Vector3 position, Quaternion rotation);
}
