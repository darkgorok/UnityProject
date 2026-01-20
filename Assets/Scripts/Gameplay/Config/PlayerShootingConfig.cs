using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Player Shooting Config", fileName = "PlayerShootingConfig")]
public class PlayerShootingConfig : ScriptableObject
{
    public float minProjectileScale = 0.3f;
    public float maxProjectileScale = 1.5f;
    public float maxChargeTime = 1.5f;
    public float projectileSpeed = 11f;
    public float shrinkFactor = 0.7f;
    public float infectionRadiusMultiplier = 0.8f;
    public float minPlayerScaleRatio = 0.2f;
    public float criticalPlayerScaleRatio = 0.18f;
    public float chargeSquashScale = 0.85f;
    public float chargeSquashDuration = 0.08f;
    public float releaseExpandDuration = 0.06f;
    public float chargeScaleSmoothTime = 0.08f;
    public float cooldownDuration = 0.1f;
}
