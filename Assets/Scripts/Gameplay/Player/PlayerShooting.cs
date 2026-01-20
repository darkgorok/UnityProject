using System;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PlayerMarker))]
public class PlayerShooting : MonoBehaviour
{
    private enum ShootingState
    {
        Idle,
        Charging,
        ShotInFlight,
        Cooldown
    }

    private sealed class ShootingStateMachine
    {
        public ShootingState State { get; private set; } = ShootingState.Idle;

        public void SetState(ShootingState next)
        {
            if (State == next)
                return;

            State = next;
        }
    }

    [Header("References")]
    [SerializeField] private ShotTuningConfig tuning;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private PlayerShootInput shootInput;
    [SerializeField] private PlayerShootGate shootGate;
    [SerializeField] private PlayerFailWatcher failWatcher;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerScaleController scaleController;
    [SerializeField] private PlayerSquashAnimator squashAnimator;
    [SerializeField] private PlayerProjectileSpawner projectileSpawner;

    private readonly ShootingStateMachine _stateMachine = new ShootingStateMachine();
    private ShotCalculator _calculator;
    private PlayerChargeController _chargeController;
    private readonly CooldownController _cooldownController = new CooldownController();

    public float AvailableScale => scaleController != null ? scaleController.AvailableScale : 0f;
    public float MinPlayerScale => scaleController != null ? scaleController.MinScale : 0f;
    public bool IsCharging => _stateMachine.State == ShootingState.Charging;
    public bool HasActiveProjectile => _activeProjectile != null;
    public bool CanBeginCharge => !_blocked && _stateMachine.State == ShootingState.Idle && _activeProjectile == null
        && scaleController != null && scaleController.AvailableScale > scaleController.MinScale && IsGameplayActive();
    public event Action<float> ShotReleased;
    public event Action ShotCompleted;
    public PlayerShootInput ShootInput => shootInput;
    public PlayerShootGate ShootGate => shootGate;
    public PlayerFailWatcher FailWatcher => failWatcher;
    public PlayerMovement Movement => movement;

    private float _previewShotScale;
    private bool _blocked;
    private Projectile _activeProjectile;
    private bool _pendingOverchargeFail;
    [Inject(Optional = true)] private IAimDirectionProvider _aimProvider;
    [Inject(Optional = true)] private IGameFlowController _gameFlow;
    [Inject(Optional = true)] private IFailController _failController;
    [Inject(Optional = true)] private IObstacleRegistry _levelManager;
    [Inject] private ITimeProvider _timeProvider;

    private void Awake()
    {
        _calculator = new ShotCalculator(tuning);
        _chargeController = new PlayerChargeController(tuning, _calculator, scaleController, squashAnimator);

        var initialScale = transform.localScale.x;
        scaleController.Initialize(initialScale, tuning.minPlayerScaleRatio, tuning.criticalPlayerScaleRatio);
    }

    private void Start()
    {
        ValidateReferences();
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

    private void Update()
    {
        if (_stateMachine.State == ShootingState.Cooldown)
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
            if (!_blocked && _activeProjectile == null && scaleController != null &&
                scaleController.AvailableScale <= scaleController.MinScale)
                TriggerFailure(ResultReason.NotEnoughSizeForShot);
            return false;
        }

        EnterCharging();
        return true;
    }

    public void TickCharge(float deltaTime)
    {
        if (_stateMachine.State != ShootingState.Charging || _blocked)
            return;

        if (_chargeController == null)
            return;

        if (!_chargeController.Tick(deltaTime, out var shouldAutoRelease))
        {
            TriggerFailure(ResultReason.NotEnoughSizeForShot);
            return;
        }

        _previewShotScale = _chargeController.PreviewShotScale;
        if (shouldAutoRelease)
        {
            ReleaseShot();
            return;
        }
    }

    public void ReleaseShot()
    {
        if (_stateMachine.State != ShootingState.Charging)
            return;

        ExitCharging();

        if (_previewShotScale <= 0f || _blocked || !IsGameplayActive())
        {
            if (scaleController != null)
                scaleController.SetBaseScaleImmediate(scaleController.AvailableScale);
            return;
        }

        var shrinkAmount = GetShrinkAmount(_previewShotScale);
        scaleController?.ApplyShrink(shrinkAmount);
        PlaySquash(1f, tuning.releaseExpandDuration);

        SpawnProjectile(_previewShotScale);
        if (!_blocked)
            ShotReleased?.Invoke(_previewShotScale);
        if (_activeProjectile != null)
            _stateMachine.SetState(ShootingState.ShotInFlight);

        if (scaleController != null && scaleController.AvailableScale <= scaleController.CriticalScale)
        {
            _pendingOverchargeFail = true;
            return;
        }

        _pendingOverchargeFail = false;
    }

    public void CancelCharge()
    {
        ExitCharging();
        _chargeController?.CancelCharge();
    }

    public void TriggerFailure(ResultReason reason)
    {
        if (_blocked)
            return;

        _blocked = true;
        _stateMachine.SetState(ShootingState.Idle);
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
        var projectileComponent = projectileSpawner != null
            ? projectileSpawner.Spawn(spawnPosition, Quaternion.identity)
            : null;

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
                _stateMachine.SetState(ShootingState.Cooldown);
                _cooldownController.Start(tuning.cooldownDuration);
            }
            else
            {
                _stateMachine.SetState(ShootingState.Idle);
            }
        }
        else
        {
            _stateMachine.SetState(ShootingState.Idle);
        }
        ShotCompleted?.Invoke();
        TryResolvePendingFail();
    }

    private void TickCooldown()
    {
        if (_cooldownController.Tick(_timeProvider.DeltaTime))
        {
            _stateMachine.SetState(ShootingState.Idle);
        }
    }

    private float GetShrinkAmount(float shotScale)
    {
        return _calculator != null ? _calculator.GetShrinkAmount(shotScale) : 0f;
    }

    private void PlaySquash(float targetMultiplier, float duration)
    {
        if (squashAnimator != null)
            squashAnimator.Play(targetMultiplier, duration);
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

    private void ValidateReferences()
    {
        projectileSpawner.ValidateReferences(this);
    }

    private void EnterCharging()
    {
        _stateMachine.SetState(ShootingState.Charging);
        _chargeController?.EnterCharge();
        _previewShotScale = _chargeController != null ? _chargeController.PreviewShotScale : 0f;
    }

    private void ExitCharging()
    {
        _stateMachine.SetState(ShootingState.Idle);
        _chargeController?.ExitCharge();
    }
}
