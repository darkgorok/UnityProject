using UnityEngine;

public class PlayerScaleController : MonoBehaviour
{
    public float AvailableScale => _availableScale;
    public float MinScale => _minScale;
    public float CriticalScale => _criticalScale;
    public float VisualMultiplier => _visualXzMultiplier;

    private float _availableScale;
    private float _minScale;
    private float _criticalScale;
    private float _baseScale;
    private float _currentBaseScale;
    private float _scaleVelocity;
    private float _visualXzMultiplier = 1f;

    public void Initialize(float initialScale, float minRatio, float criticalRatio)
    {
        _availableScale = initialScale;
        _minScale = initialScale * minRatio;
        _criticalScale = Mathf.Max(initialScale * criticalRatio, _minScale);
        SetBaseScaleImmediate(_availableScale);
    }

    public float GetTemporaryScale(float shrinkAmount)
    {
        return Mathf.Max(_availableScale - shrinkAmount, _minScale);
    }

    public void ApplyShrink(float shrinkAmount)
    {
        _availableScale = Mathf.Max(_availableScale - shrinkAmount, _minScale);
        SetBaseScaleImmediate(_availableScale);
    }

    public void SetBaseScaleImmediate(float scale)
    {
        _baseScale = scale;
        _currentBaseScale = scale;
        _scaleVelocity = 0f;
        ApplyScale();
    }

    public void SetBaseScaleSmoothed(float scale, float smoothTime)
    {
        _baseScale = scale;
        if (smoothTime <= 0f)
        {
            _currentBaseScale = scale;
            _scaleVelocity = 0f;
            ApplyScale();
            return;
        }

        _currentBaseScale = Mathf.SmoothDamp(
            _currentBaseScale,
            _baseScale,
            ref _scaleVelocity,
            smoothTime);
        ApplyScale();
    }

    public void SetVisualMultiplier(float multiplier)
    {
        _visualXzMultiplier = multiplier;
        ApplyScale();
    }

    private void ApplyScale()
    {
        transform.localScale = new Vector3(
            _currentBaseScale * _visualXzMultiplier,
            _currentBaseScale,
            _currentBaseScale * _visualXzMultiplier);
    }
}
