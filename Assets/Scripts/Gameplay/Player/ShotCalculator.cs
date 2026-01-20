using UnityEngine;

public sealed class ShotCalculator
{
    private readonly ShotTuningConfig _tuning;

    public ShotCalculator(ShotTuningConfig tuning)
    {
        _tuning = tuning;
    }

    public float GetMaxShotScale(float availableScale, float minScale)
    {
        if (_tuning == null)
            return 0f;

        return Mathf.Max((availableScale - minScale) / _tuning.shrinkFactor, 0f);
    }

    public float CalculateNextShotScale(float chargeTime, float maxShotScale)
    {
        if (_tuning == null)
            return 0f;

        var chargeRatio = Mathf.Clamp01(chargeTime / _tuning.maxChargeTime);
        var nextShotScale = Mathf.Lerp(_tuning.minProjectileScale, _tuning.maxProjectileScale, chargeRatio);
        return Mathf.Min(nextShotScale, maxShotScale);
    }

    public bool ShouldAutoRelease(float chargeTime, float nextShotScale, float maxShotScale)
    {
        if (_tuning == null)
            return false;

        return chargeTime >= _tuning.maxChargeTime ||
               Mathf.Abs(nextShotScale - maxShotScale) <= 0.0001f;
    }

    public float GetShrinkAmount(float shotScale)
    {
        if (_tuning == null)
            return 0f;

        return shotScale * _tuning.shrinkFactor;
    }
}
