using System;
using UnityEngine;
using Zenject;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PlayerMarker))]
public class PlayerShooting : MonoBehaviour, ITickable
{
    private enum ShootingState
    {
        Idle,
        Charging,
        ShotInFlight,
        Cooldown
    }

    [Header("References")]
    [SerializeField] private ProjectileFactory projectileFactory;
    [SerializeField] private PlayerShootingConfig config;
    [SerializeField] private Collider playerCollider;

    [Header("Shot Tuning")]
    [SerializeField] private float minProjectileScale = 0.3f;
    [SerializeField] private float maxProjectileScale = 1.5f;
    [SerializeField] private float maxChargeTime = 1.5f;
    [SerializeField] private float projectileSpeed = 11f;
    [SerializeField] private float shrinkFactor = 0.7f;
    [SerializeField] private float infectionRadiusMultiplier = 0.8f;

    [Header("Player Size")]
    [SerializeField] [Range(0.05f, 0.5f)] private float minPlayerScaleRatio = 0.2f;
    [SerializeField] [Range(0.1f, 0.3f)] private float criticalPlayerScaleRatio = 0.18f;
    [Header("Charge Squash")]
    [SerializeField] private float chargeSquashScale = 0.85f;
    [SerializeField] private float chargeSquashDuration = 0.08f;
    [SerializeField] private float releaseExpandDuration = 0.06f;
    [SerializeField] private float chargeScaleSmoothTime = 0.08f;
    [SerializeField] private float cooldownDuration = 0.1f;

    public float AvailableScale => _availableScale;
    public float MinPlayerScale => _minPlayerScale;
    public bool IsCharging => _state == ShootingState.Charging;
    public bool HasActiveProjectile => _activeProjectile != null;
    public bool CanBeginCharge => !_blocked && _state == ShootingState.Idle && _activeProjectile == null
        && _availableScale > _minPlayerScale && IsGameplayActive();

    private float _initialScale;
    private float _availableScale;
    private float _minPlayerScale;
    private float _criticalPlayerScale;
    private ShootingState _state = ShootingState.Idle;
    private float _chargeTime;
    private float _previewShotScale;
    private bool _blocked;
    private Projectile _activeProjectile;
    private bool _pendingOverchargeFail;
    private float _cooldownTimer;
    [Inject(Optional = true)] private IAimDirectionProvider _aimProvider;
    [Inject] private IProjectileFactory _projectileFactory;
    [Inject(Optional = true)] private IGameFlowController _gameFlow;
    [Inject(Optional = true)] private SignalBus _signalBus;
    [Inject(Optional = true)] private IFailController _failController;
    [Inject(Optional = true)] private IObstacleRegistry _levelManager;
    [Inject(Optional = true)] private ITimeProvider _timeProvider;
    private float _baseScale;
    private float _currentBaseScale;
    private float _scaleVelocity;
    private float _visualXzMultiplier = 1f;
    private Tween _squashTween;

    private void Awake()
    {
        ApplyTuning();
        _initialScale = transform.localScale.x;
        _availableScale = _initialScale;
        _minPlayerScale = _initialScale * minPlayerScaleRatio;
        _criticalPlayerScale = Mathf.Max(_initialScale * criticalPlayerScaleRatio, _minPlayerScale);

        if (playerCollider != null)
            playerCollider.isTrigger = false;

        _baseScale = _availableScale;
        _currentBaseScale = _baseScale;
        ApplyScale();
    }

    private void OnEnable()
    {
        if (_gameFlow != null)
        {
            _gameFlow.StateChanged += HandleStateChanged;
            if (_gameFlow.State == GameFlowState.Win || _gameFlow.State == GameFlowState.Lose)
                SetShootingEnabled(false);
            else
                SetShootingEnabled(true);
        }

        if (_signalBus != null)
            _signalBus.Subscribe<PathClearedSignal>(HandlePathCleared);
    }

    private void OnDisable()
    {
        if (_gameFlow != null)
            _gameFlow.StateChanged -= HandleStateChanged;
        if (_signalBus != null)
            _signalBus.TryUnsubscribe<PathClearedSignal>(HandlePathCleared);
    }

    public void Tick()
    {
        if (_state != ShootingState.Cooldown)
            return;

        _cooldownTimer -= _timeProvider != null ? _timeProvider.DeltaTime : Time.deltaTime;
        if (_cooldownTimer <= 0f)
        {
            _cooldownTimer = 0f;
            _state = ShootingState.Idle;
        }
    }


    public void SetShootingEnabled(bool enabled)
    {
        _blocked = !enabled;
        if (_blocked)
            CancelCharge();
    }

    public bool BeginCharge()
    {
        if (!CanBeginCharge)
        {
            if (!_blocked && _availableScale <= _minPlayerScale)
                TriggerFailure(ResultReason.NotEnoughSizeForShot);
            return false;
        }

        _state = ShootingState.Charging;
        _chargeTime = 0f;
        _previewShotScale = minProjectileScale;
        PlaySquash(chargeSquashScale, chargeSquashDuration);
        return true;
    }

    public void TickCharge(float deltaTime)
    {
        if (_state != ShootingState.Charging || _blocked)
            return;

        _chargeTime += deltaTime;
        var chargeRatio = Mathf.Clamp01(_chargeTime / maxChargeTime);
        var nextShotScale = Mathf.Lerp(minProjectileScale, maxProjectileScale, chargeRatio);
        var maxShotScaleBySize = Mathf.Max((_availableScale - _minPlayerScale) / shrinkFactor, 0f);

        if (maxShotScaleBySize <= 0f)
        {
            TriggerFailure(ResultReason.NotEnoughSizeForShot);
            return;
        }

        nextShotScale = Mathf.Min(nextShotScale, maxShotScaleBySize);
        _previewShotScale = nextShotScale;

        var shrinkAmount = _previewShotScale * shrinkFactor;
        var temporaryScale = Mathf.Max(_availableScale - shrinkAmount, _minPlayerScale);

        if (_chargeTime >= maxChargeTime ||
            Mathf.Abs(nextShotScale - maxShotScaleBySize) <= 0.0001f)
        {
            ReleaseShot();
            return;
        }

        SetBaseScaleSmoothed(temporaryScale);
    }

    public void ReleaseShot()
    {
        if (_state != ShootingState.Charging)
            return;

        _state = ShootingState.Idle;
        _chargeTime = 0f;

        if (_previewShotScale <= 0f || _blocked || !IsGameplayActive())
        {
            SetBaseScaleImmediate(_availableScale);
            return;
        }

        var shrinkAmount = _previewShotScale * shrinkFactor;
        _availableScale = Mathf.Max(_availableScale - shrinkAmount, _minPlayerScale);
        SetBaseScaleImmediate(_availableScale);
        PlaySquash(1f, releaseExpandDuration);

        SpawnProjectile(_previewShotScale);
        if (!_blocked)
            _signalBus?.Fire(new ShotReleasedSignal(_previewShotScale));
        if (_activeProjectile != null)
            _state = ShootingState.ShotInFlight;

        if (_availableScale <= _criticalPlayerScale)
        {
            _pendingOverchargeFail = true;
            return;
        }

        _pendingOverchargeFail = false;
    }

    public void CancelCharge()
    {
        _state = ShootingState.Idle;
        _chargeTime = 0f;
        SetBaseScaleImmediate(_availableScale);
        PlaySquash(1f, releaseExpandDuration);
    }

    public void TriggerFailure(ResultReason reason)
    {
        if (_blocked)
            return;

        _blocked = true;
        _state = ShootingState.Idle;
        _cooldownTimer = 0f;
        _pendingOverchargeFail = false;
        CancelCharge();
        if (_failController != null)
            _failController.TryFail(reason);
        else
            _signalBus?.Fire(new LoseSignal(reason));
    }

    private void HandleStateChanged(GameFlowState state)
    {
        if (state == GameFlowState.Win || state == GameFlowState.Lose)
            SetShootingEnabled(false);
        else
            SetShootingEnabled(true);
    }

    private void SpawnProjectile(float shotScale)
    {
        if (_activeProjectile != null || _blocked)
            return;

        var spawnPosition = transform.position;
        Projectile projectileComponent = null;

        if (_projectileFactory != null)
        {
            projectileComponent = _projectileFactory.Create(spawnPosition, Quaternion.identity);
        }
        else if (projectileFactory != null)
        {
            projectileComponent = projectileFactory.Create(spawnPosition, Quaternion.identity);
        }

        if (projectileComponent == null)
            return;

        projectileComponent.transform.localScale = Vector3.one * shotScale;

        var projectileCollider = projectileComponent.Collider;
        if (projectileCollider == null)
            projectileCollider = projectileComponent.gameObject.AddComponent<SphereCollider>();
        if (playerCollider != null)
            Physics.IgnoreCollision(playerCollider, projectileCollider);

        var direction = _aimProvider != null
            ? _aimProvider.GetDirection(spawnPosition)
            : transform.forward;

        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.forward;

        var infectionRadius = shotScale * infectionRadiusMultiplier;

        projectileComponent.Completed -= HandleProjectileComplete;
        projectileComponent.Completed += HandleProjectileComplete;
        projectileComponent.transform.position = transform.position;
        projectileComponent.Initialize(direction, projectileSpeed, shotScale, infectionRadius);
        _activeProjectile = projectileComponent;
    }

    private void HandleProjectileComplete()
    {
        _activeProjectile = null;
        if (!_blocked)
        {
            if (cooldownDuration > 0f)
            {
                _state = ShootingState.Cooldown;
                _cooldownTimer = cooldownDuration;
            }
            else
            {
                _state = ShootingState.Idle;
            }
        }
        else
        {
            _state = ShootingState.Idle;
        }
        _signalBus?.Fire(new ShotCompletedSignal());
        TryResolvePendingFail();
    }

    private void OnDestroy()
    {
        if (_squashTween != null)
            _squashTween.Kill();
    }

    private void SetBaseScaleImmediate(float scale)
    {
        _baseScale = scale;
        _currentBaseScale = scale;
        _scaleVelocity = 0f;
        ApplyScale();
    }

    private void SetBaseScaleSmoothed(float scale)
    {
        _baseScale = scale;
        if (chargeScaleSmoothTime <= 0f)
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
            chargeScaleSmoothTime);
        ApplyScale();
    }

    private void ApplyScale()
    {
        transform.localScale = new Vector3(
            _currentBaseScale * _visualXzMultiplier,
            _currentBaseScale,
            _currentBaseScale * _visualXzMultiplier);
    }

    private void PlaySquash(float targetMultiplier, float duration)
    {
        if (duration <= 0f)
        {
            _visualXzMultiplier = targetMultiplier;
            ApplyScale();
            return;
        }

        if (_squashTween != null)
            _squashTween.Kill();

        _squashTween = DOTween.To(
                () => _visualXzMultiplier,
                value =>
                {
                    _visualXzMultiplier = value;
                    ApplyScale();
                },
                targetMultiplier,
                duration)
            .SetEase(Ease.OutQuad);
    }

    private bool IsGameplayActive()
    {
        return _gameFlow == null ||
               _gameFlow.State == GameFlowState.Play ||
               _gameFlow.State == GameFlowState.Init;
    }

    private void HandlePathCleared()
    {
        _pendingOverchargeFail = false;
    }

    private void TryResolvePendingFail()
    {
        if (!_pendingOverchargeFail)
            return;

        if (_levelManager != null && _levelManager.IsPathCleared)
        {
            _pendingOverchargeFail = false;
            return;
        }

        TriggerFailure(ResultReason.Overcharged);
        _pendingOverchargeFail = false;
    }

    private void ApplyTuning()
    {
        if (config == null)
            return;

        minProjectileScale = config.minProjectileScale;
        maxProjectileScale = config.maxProjectileScale;
        maxChargeTime = config.maxChargeTime;
        projectileSpeed = config.projectileSpeed;
        shrinkFactor = config.shrinkFactor;
        infectionRadiusMultiplier = config.infectionRadiusMultiplier;
        minPlayerScaleRatio = config.minPlayerScaleRatio;
        criticalPlayerScaleRatio = config.criticalPlayerScaleRatio;
        chargeSquashScale = config.chargeSquashScale;
        chargeSquashDuration = config.chargeSquashDuration;
        releaseExpandDuration = config.releaseExpandDuration;
        chargeScaleSmoothTime = config.chargeScaleSmoothTime;
        cooldownDuration = config.cooldownDuration;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (playerCollider == null)
            playerCollider = GetComponent<Collider>();
    }
#endif
}
