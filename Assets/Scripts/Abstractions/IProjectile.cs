using System;
using UnityEngine;

public interface IProjectile
{
    event Action Completed;
    void Initialize(Vector3 direction, float speed, float size, float infectionRadius);
}
