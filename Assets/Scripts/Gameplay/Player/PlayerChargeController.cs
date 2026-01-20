using UnityEngine;

public sealed class PlayerChargeController
{
    private readonly ShotTuningConfig _tuning;
    private readonly ShotCalculator _calculator;
    private readonly PlayerScaleController _scaleController;
    private readonly PlayerSquashAnimator _squashAnimator;

    private float _chargeTime;
    private float _previewShotScale;

    public float ChargeTime => _chargeTime;
    public float PreviewShotScale => _previewShotScale;

    public PlayerChargeController(
        ShotTuningConfig tuning,
        ShotCalculator calculator,
        PlayerScaleController scaleController,
        PlayerSquashAnimator squashAnimator)
    {
        _tuning = tuning;
        _calculator = calculator;
        _scaleController = scaleController;
        _squashAnimator = squashAnimator;
    }

    public void EnterCharge()
    {
        _chargeTime = 0f;
        _previewShotScale = _tuning != null ? _tuning.minProjectileScale : 0f;
        _squashAnimator?.Play(_tuning != null ? _tuning.chargeSquashScale : 1f,
            _tuning != null ? _tuning.chargeSquashDuration : 0f);
    }

    public void ExitCharge()
    {
        _chargeTime = 0f;
    }

    public void CancelCharge()
    {
        ExitCharge();
        if (_scaleController != null)
            _scaleController.SetBaseScaleImmediate(_scaleController.AvailableScale);
        _squashAnimator?.Play(1f, _tuning != null ? _tuning.releaseExpandDuration : 0f);
    }

    public bool Tick(float deltaTime, out bool shouldAutoRelease)
    {
        shouldAutoRelease = false;
        if (_tuning == null || _calculator == null || _scaleController == null)
            return false;

        _chargeTime += deltaTime;
        var maxShotScale = _calculator.GetMaxShotScale(_scaleController.AvailableScale, _scaleController.MinScale);
        if (maxShotScale <= 0f)
            return false;

        _previewShotScale = _calculator.CalculateNextShotScale(_chargeTime, maxShotScale);
        shouldAutoRelease = _calculator.ShouldAutoRelease(_chargeTime, _previewShotScale, maxShotScale);

        var temporaryScale = _scaleController.GetTemporaryScale(_calculator.GetShrinkAmount(_previewShotScale));
        _scaleController.SetBaseScaleSmoothed(temporaryScale, _tuning.chargeScaleSmoothTime);
        return true;
    }
}
