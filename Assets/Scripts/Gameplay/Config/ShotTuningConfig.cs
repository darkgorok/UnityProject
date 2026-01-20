using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Shot Tuning Config", fileName = "ShotTuningConfig")]
public class ShotTuningConfig : ScriptableObject
{
    [Header("Projectile Scale")]
    [Tooltip("Smallest projectile size when starting charge.")]
    public float minProjectileScale = 0.3f;
    [Tooltip("Largest projectile size at full charge.")]
    public float maxProjectileScale = 1.5f;

    [Header("Charge Timing")]
    [Tooltip("Time to reach max projectile scale.")]
    public float maxChargeTime = 1.5f;

    [Header("Projectile Movement")]
    [Tooltip("Projectile speed after release.")]
    public float projectileSpeed = 11f;

    [Header("Player Size Cost")]
    [Tooltip("How much player shrinks per unit of shot scale.")]
    public float shrinkFactor = 0.7f;
    [Tooltip("Radius = shotScale * infectionRadiusMultiplier.")]
    public float infectionRadiusMultiplier = 0.8f;

    [Header("Player Size Limits")]
    [Tooltip("Minimal player scale as ratio of initial scale.")]
    public float minPlayerScaleRatio = 0.2f;
    [Tooltip("Critical scale for overcharge failure.")]
    public float criticalPlayerScaleRatio = 0.18f;

    [Header("Charge Squash")]
    [Tooltip("Squash multiplier while charging.")]
    public float chargeSquashScale = 0.85f;
    [Tooltip("Duration of charge squash.")]
    public float chargeSquashDuration = 0.08f;
    [Tooltip("Duration of release expand.")]
    public float releaseExpandDuration = 0.06f;
    [Tooltip("Smooth time for scale easing during charge.")]
    public float chargeScaleSmoothTime = 0.08f;

    [Header("Cooldown")]
    [Tooltip("Cooldown after projectile release.")]
    public float cooldownDuration = 0.1f;
}
