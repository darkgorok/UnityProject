using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Projectile Config", fileName = "ProjectileConfig")]
public class ProjectileConfig : ScriptableObject
{
    [Header("Lifetime")]
    [Tooltip("Seconds before projectile despawns if it hits nothing.")]
    public float lifeTime = 5f;
    [Header("Infection Detection")]
    [Tooltip("Buffer size for overlap sphere queries.")]
    public int overlapBufferSize = 16;
    [Tooltip("Layers affected by infection.")]
    public LayerMask obstacleLayers = ~0;
    [Header("Infection VFX")]
    [Tooltip("Lifetime of infection VFX.")]
    public float infectionVfxDuration = 0.25f;
    [Tooltip("Visual scale multiplier for infection VFX.")]
    public float infectionVfxScaleMultiplier = 2f;
}
