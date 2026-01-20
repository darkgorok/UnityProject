using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Projectile Config", fileName = "ProjectileConfig")]
public class ProjectileConfig : ScriptableObject
{
    [Header("Lifetime")]
    public float lifeTime = 5f;
    [Header("Infection Detection")]
    public int overlapBufferSize = 16;
    public LayerMask obstacleLayers = ~0;
    [Header("Infection VFX")]
    public float infectionVfxDuration = 0.25f;
    public float infectionVfxScaleMultiplier = 2f;
}
