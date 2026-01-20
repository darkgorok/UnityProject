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

    [System.Serializable]
    private struct ShotTuning
    {
        public float minProjectileScale;
        public float maxProjectileScale;
        public float maxChargeTime;
        public float projectileSpeed;
        public float shrinkFactor;
        public float infectionRadiusMultiplier;
        [Range(0.05f, 0.5f)] public float minPlayerScaleRatio;
        [Range(0.1f, 0.3f)] public float criticalPlayerScaleRatio;
        public float chargeSquashScale;
        public float chargeSquashDuration;
        public float releaseExpandDuration;
        public float chargeScaleSmoothTime;
        public float cooldownDuration;
    }

    [Header("References")]
    [SerializeField] private ProjectileFactory projectileFactory;
    [SerializeField] private PlayerShootingConfig config;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private PlayerShootInput shootInput;
    [SerializeField] private PlayerShootGate shootGate;
    [SerializeField] private PlayerFailWatcher failWatcher;
    [SerializeField] private PlayerMovement movement;

    [Header("Tuning")]
    [SerializeField] private ShotTuning tuning = new ShotTuning
    {
        minProjectileScale = 0.3f,
        maxProjectileScale = 1.5f,
        maxChargeTime = 1.5f,
        projectileSpeed = 11f,
        shrinkFactor = 0.7f,
        infectionRadiusMultiplier = 0.8f,
        minPlayerScaleRatio = 0.2f,
        criticalPlayerScaleRatio = 0.18f,
        chargeSquashScale = 0.85f,
        chargeSquashDuration = 0.08f,
        releaseExpandDuration = 0.06f,
        chargeScaleSmoothTime = 0.08f,
        cooldownDuration = 0.1f
    };

    public float AvailableScale => _availableScale;
    public float MinPlayerScale => _minPlayerScale;
    public bool IsCharging => _state == ShootingState.Charging;
    public bool HasActiveProjectile => _activeProjectile != null;
    public bool CanBeginCharge => !_blocked && _state == ShootingState.Idle && _activeProjectile == null
        && _availableScale > _minPlayerScale && IsGameplayActive();
    public event Action<float> ShotReleased;
    public event Action ShotCompleted;
    public PlayerShootInput ShootInput => shootInput;
    public PlayerShootGate ShootGate => shootGate;
    public PlayerFailWatcher FailWatcher => failWatcher;
    public PlayerMovement Movement => movement;
    public ProjectileFactory ProjectileFactoryComponent => projectileFactory;

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
    [Inject(Optional = true)] private IFailController _failController;
    [Inject(Optional = true)] private IObstacleRegistry _levelManager;
    [Inject] private ITimeProvider _timeProvider;
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
        _minPlayerScale = _initialScale * tuning.minPlayerScaleRatio;
        _criticalPlayerScale = Mathf.Max(_initialScale * tuning.criticalPlayerScaleRatio, _minPlayerScale);

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

        if (_levelManager != null)
            _levelManager.PathCleared += HandlePathCleared;
    }

    private void OnDisable()
    {
        if (_gameFlow != null)
            _gameFlow.StateChanged -= HandleStateChanged;
        if (_levelManager != null)
            _levelManager.PathCleared -= HandlePathCleared;
    }

    public void Tick()
    {
        if (_state == ShootingState.Cooldown)
            TickCooldown();
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
            if (!_blocked && _activeProjectile == null && _availableScale <= _minPlayerScale)
                TriggerFailure(ResultReason.NotEnoughSizeForShot);
            return false;
        }

        EnterCharging();
        return true;
    }

    public void TickCharge(float deltaTime)
    {
        if (_state != ShootingState.Charging || _blocked)
            return;

        _chargeTime += deltaTime;
        UpdateCharging();
    }

    public void ReleaseShot()
    {
        if (_state != ShootingState.Charging)
            return;

        ExitCharging();

        if (_previewShotScale <= 0f || _blocked || !IsGameplayActive())
        {
            SetBaseScaleImmediate(_availableScale);
            return;
        }

        var shrinkAmount = GetShrinkAmount(_previewShotScale);
        _availableScale = Mathf.Max(_availableScale - shrinkAmount, _minPlayerScale);
        SetBaseScaleImmediate(_availableScale);
        PlaySquash(1f, tuning.releaseExpandDuration);

        SpawnProjectile(_previewShotScale);
        if (!_blocked)
            ShotReleased?.Invoke(_previewShotScale);
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
        ExitCharging();
        SetBaseScaleImmediate(_availableScale);
        PlaySquash(1f, tuning.releaseExpandDuration);
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
        else if (_gameFlow != null)
            _gameFlow.SetLose(reason.ToString());
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
        {
            Debug.LogError("PlayerShooting: Projectile collider is missing.", projectileComponent);
            return;
        }
        if (playerCollider != null)
            Physics.IgnoreCollision(playerCollider, projectileCollider);

        var direction = _aimProvider != null
            ? _aimProvider.GetDirection(spawnPosition)
            : transform.forward;

        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.forward;

        var infectionRadius = shotScale * tuning.infectionRadiusMultiplier;

        projectileComponent.Completed -= HandleProjectileComplete;
        projectileComponent.Completed += HandleProjectileComplete;
        projectileComponent.transform.position = transform.position;
        projectileComponent.Initialize(direction, tuning.projectileSpeed, shotScale, infectionRadius);
        _activeProjectile = projectileComponent;
    }

    private void HandleProjectileComplete()
    {
        _activeProjectile = null;
        if (!_blocked)
        {
            if (tuning.cooldownDuration > 0f)
            {
                _state = ShootingState.Cooldown;
                _cooldownTimer = tuning.cooldownDuration;
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
        ShotCompleted?.Invoke();
        TryResolvePendingFail();
    }

    private void TickCooldown()
    {
        _cooldownTimer -= _timeProvider.DeltaTime;
        if (_cooldownTimer <= 0f)
        {
            _cooldownTimer = 0f;
            _state = ShootingState.Idle;
        }
    }

    private float GetMaxShotScaleBySize()
    {
        return Mathf.Max((_availableScale - _minPlayerScale) / tuning.shrinkFactor, 0f);
    }

    private float GetShrinkAmount(float shotScale)
    {
        return shotScale * tuning.shrinkFactor;
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
        if (tuning.chargeScaleSmoothTime <= 0f)
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
            tuning.chargeScaleSmoothTime);
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

        tuning.minProjectileScale = config.minProjectileScale;
        tuning.maxProjectileScale = config.maxProjectileScale;
        tuning.maxChargeTime = config.maxChargeTime;
        tuning.projectileSpeed = config.projectileSpeed;
        tuning.shrinkFactor = config.shrinkFactor;
        tuning.infectionRadiusMultiplier = config.infectionRadiusMultiplier;
        tuning.minPlayerScaleRatio = config.minPlayerScaleRatio;
        tuning.criticalPlayerScaleRatio = config.criticalPlayerScaleRatio;
        tuning.chargeSquashScale = config.chargeSquashScale;
        tuning.chargeSquashDuration = config.chargeSquashDuration;
        tuning.releaseExpandDuration = config.releaseExpandDuration;
        tuning.chargeScaleSmoothTime = config.chargeScaleSmoothTime;
        tuning.cooldownDuration = config.cooldownDuration;
    }

    private void EnterCharging()
    {
        _state = ShootingState.Charging;
        _chargeTime = 0f;
        _previewShotScale = tuning.minProjectileScale;
        PlaySquash(tuning.chargeSquashScale, tuning.chargeSquashDuration);
    }

    private void ExitCharging()
    {
        _state = ShootingState.Idle;
        _chargeTime = 0f;
    }

    private void UpdateCharging()
    {
        var maxShotScaleBySize = GetMaxShotScaleBySize();
        if (maxShotScaleBySize <= 0f)
        {
            TriggerFailure(ResultReason.NotEnoughSizeForShot);
            return;
        }

        var nextShotScale = CalculateNextShotScale(maxShotScaleBySize);
        _previewShotScale = nextShotScale;

        if (ShouldAutoRelease(nextShotScale, maxShotScaleBySize))
        {
            ReleaseShot();
            return;
        }

        var temporaryScale = Mathf.Max(_availableScale - GetShrinkAmount(_previewShotScale), _minPlayerScale);
        SetBaseScaleSmoothed(temporaryScale);
    }

    private float CalculateNextShotScale(float maxShotScaleBySize)
    {
        var chargeRatio = Mathf.Clamp01(_chargeTime / tuning.maxChargeTime);
        var nextShotScale = Mathf.Lerp(tuning.minProjectileScale, tuning.maxProjectileScale, chargeRatio);
        return Mathf.Min(nextShotScale, maxShotScaleBySize);
    }

    private bool ShouldAutoRelease(float nextShotScale, float maxShotScaleBySize)
    {
        return _chargeTime >= tuning.maxChargeTime ||
               Mathf.Abs(nextShotScale - maxShotScaleBySize) <= 0.0001f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (playerCollider == null)
            playerCollider = GetComponent<Collider>();
        if (projectileFactory == null)
            projectileFactory = GetComponent<ProjectileFactory>();
        if (shootInput == null)
            shootInput = GetComponent<PlayerShootInput>();
        if (shootGate == null)
            shootGate = GetComponent<PlayerShootGate>();
        if (failWatcher == null)
            failWatcher = GetComponent<PlayerFailWatcher>();
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
    }
#endif
}
