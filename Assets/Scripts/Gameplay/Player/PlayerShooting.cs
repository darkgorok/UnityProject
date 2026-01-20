using System;
using UnityEngine;
using Zenject;

public class PlayerShooting : MonoBehaviour, IPlayerShooting
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
    [SerializeField] private PlayerScaleController scaleController;
    [SerializeField] private PlayerSquashAnimator squashAnimator;
    [SerializeField] private PlayerProjectileSpawner projectileSpawner;

    private readonly ShootingStateMachine _stateMachine = new ShootingStateMachine();
    private ShotCalculator _calculator;
    private PlayerChargeController _chargeController;
    private readonly CooldownController _cooldownController = new CooldownController();

    private event Action<float> _shotReleased = delegate { };
    private event Action _shotCompleted = delegate { };
    private bool CanBeginCharge => !_blocked && _stateMachine.State == ShootingState.Idle && _activeProjectile == null
        && scaleController.AvailableScale > scaleController.MinScale && IsGameplayActive();

    private float _previewShotScale;
    private bool _blocked;
    private Projectile _activeProjectile;
    private bool _pendingOverchargeFail;
    [Inject] private IAimDirectionProvider _aimProvider;
    [Inject] private IGameFlowController _gameFlow;
    [Inject] private IFailController _failController;
    [Inject] private IObstacleRegistry _levelManager;
    [Inject] private ITimeProvider _timeProvider;

    float IPlayerShooting.AvailableScale => scaleController.AvailableScale;
    float IPlayerShooting.MinPlayerScale => scaleController.MinScale;
    bool IPlayerShooting.IsCharging => _stateMachine.State == ShootingState.Charging;
    bool IPlayerShooting.HasActiveProjectile => _activeProjectile != null;
    event Action<float> IPlayerShooting.ShotReleased
    {
        add => _shotReleased += value;
        remove => _shotReleased -= value;
    }
    event Action IPlayerShooting.ShotCompleted
    {
        add => _shotCompleted += value;
        remove => _shotCompleted -= value;
    }

    private void Awake()
    {
        _calculator = new ShotCalculator(tuning);
        _chargeController = new PlayerChargeController(tuning, _calculator, scaleController, squashAnimator);

        var initialScale = transform.localScale.x;
        scaleController.Initialize(initialScale, tuning.minPlayerScaleRatio, tuning.criticalPlayerScaleRatio);
    }

    private void OnEnable()
    {
        _gameFlow.StateChanged += HandleStateChanged;
        if (_gameFlow.State == GameFlowState.Win || _gameFlow.State == GameFlowState.Lose)
            SetShootingEnabled(false);
        else
            SetShootingEnabled(true);

        _levelManager.PathCleared += HandlePathCleared;
    }

    private void OnDisable()
    {
        _gameFlow.StateChanged -= HandleStateChanged;
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
            if (!_blocked && _activeProjectile == null &&
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
            scaleController.SetBaseScaleImmediate(scaleController.AvailableScale);
            return;
        }

        var shrinkAmount = GetShrinkAmount(_previewShotScale);
        scaleController.ApplyShrink(shrinkAmount);
        PlaySquash(1f, tuning.releaseExpandDuration);

        SpawnProjectile(_previewShotScale);
        if (!_blocked)
            _shotReleased(_previewShotScale);
        if (_activeProjectile != null)
            _stateMachine.SetState(ShootingState.ShotInFlight);

        if (scaleController.AvailableScale <= scaleController.CriticalScale)
        {
            _pendingOverchargeFail = true;
            return;
        }

        _pendingOverchargeFail = false;
    }

    public void CancelCharge()
    {
        ExitCharging();
        _chargeController.CancelCharge();
    }

    public void TriggerFailure(ResultReason reason)
    {
        if (_blocked)
            return;

        _blocked = true;
        _stateMachine.SetState(ShootingState.Idle);
        _pendingOverchargeFail = false;
        CancelCharge();
        _failController.TryFail(reason);
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
        var projectileComponent = projectileSpawner.Spawn(spawnPosition, Quaternion.identity);

        projectileComponent.transform.localScale = Vector3.one * shotScale;

        var projectileCollider = projectileComponent.Collider;
        Physics.IgnoreCollision(playerCollider, projectileCollider);

        var direction = _aimProvider.GetDirection(spawnPosition);

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
        _shotCompleted();
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
        return _calculator.GetShrinkAmount(shotScale);
    }

    private void PlaySquash(float targetMultiplier, float duration)
    {
        squashAnimator.Play(targetMultiplier, duration);
    }

    private bool IsGameplayActive()
    {
        return _gameFlow.State == GameFlowState.Play ||
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

        if (_levelManager.IsPathCleared)
        {
            _pendingOverchargeFail = false;
            return;
        }

        TriggerFailure(ResultReason.Overcharged);
        _pendingOverchargeFail = false;
    }

    private void EnterCharging()
    {
        _stateMachine.SetState(ShootingState.Charging);
        _chargeController.EnterCharge();
        _previewShotScale = _chargeController.PreviewShotScale;
    }

    private void ExitCharging()
    {
        _stateMachine.SetState(ShootingState.Idle);
        _chargeController.ExitCharge();
    }
}
