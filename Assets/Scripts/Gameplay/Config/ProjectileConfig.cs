using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Projectile Config", fileName = "ProjectileConfig")]
public class ProjectileConfig : ScriptableObject
{
    public float lifeTime = 5f;
    public int overlapBufferSize = 16;
    public LayerMask obstacleLayers = ~0;
    public float infectionVfxDuration = 0.25f;
    public float infectionVfxScaleMultiplier = 2f;
}
