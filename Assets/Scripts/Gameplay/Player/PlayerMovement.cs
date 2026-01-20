using UnityEngine;
using Zenject;
using DG.Tweening;

public class PlayerMovement : MonoBehaviour, ITickable
{
    private enum MovementState
    {
        Idle,
        Moving,
        Reached
    }
    [Header("References")]
    [SerializeField] private Transform goalTransform;
    [SerializeField] private PlayerMovementConfig config;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float doorReachDistance = 1.5f;
    [SerializeField] private float jumpHeight = 0.6f;
    [SerializeField] private float hopDuration = 0.4f;
    [Header("Jump Squash")]
    [SerializeField] private float squashScaleY = 0.75f;
    [SerializeField] private float stretchScaleY = 1.2f;
    [SerializeField] private float squashDuration = 0.08f;
    [SerializeField] private float stretchDuration = 0.1f;

    [Inject] private IObstacleRegistry _levelManager;
    [Inject(Optional = true)] private IDoor _door;
    [Inject(Optional = true, Id = "Goal")] private Transform _injectedGoal;
    [Inject(Optional = true)] private IGameFlowController _gameFlow;
    [Inject] private ITimeProvider _timeProvider;
    [Inject(Optional = true)] private PlayerShooting _shooting;

    private bool _inHop;
    private float _hopTimer;
    private Vector3 _hopStart;
    private Vector3 _hopEnd;
    private float _baseY;
    private Vector3 _defaultScale;
    private Sequence _squashSequence;
    private MovementState _state = MovementState.Idle;

    private void Awake()
    {
        ApplyTuning();
        _defaultScale = transform.localScale;
        if (goalTransform == null && _injectedGoal != null)
            goalTransform = _injectedGoal;

    }

    private void OnDestroy()
    {
        if (_squashSequence != null)
            _squashSequence.Kill();
    }

    private void OnEnable()
    {
        if (_levelManager != null)
            _levelManager.PathCleared += OnPathCleared;
        if (_levelManager != null && _levelManager.IsPathCleared)
            StartMoving();
    }

    private void OnDisable()
    {
        if (_levelManager != null)
            _levelManager.PathCleared -= OnPathCleared;
    }

    public void Tick()
    {
        if (!IsGameplayActive())
            return;

        switch (_state)
        {
            case MovementState.Idle:
                TryStartMoving();
                break;
            case MovementState.Moving:
                TickMoving();
                break;
        }
    }


    private void StartMoving()
    {
        _state = MovementState.Moving;
        _inHop = false;
        _defaultScale = transform.localScale;
        _shooting?.SetShootingEnabled(false);
    }

    private bool IsGameplayActive()
    {
        return _gameFlow == null || _gameFlow.State == GameFlowState.Play;
    }

    private void TryStartMoving()
    {
        if (_levelManager != null && _levelManager.IsPathCleared)
            StartMoving();
    }

    private void TickMoving()
    {
        if (!HasGoal())
            return;

        if (!_inHop)
        {
            StartNextHop();
            return;
        }

        _hopTimer += _timeProvider.DeltaTime;
        var t = Mathf.Clamp01(_hopTimer / Mathf.Max(0.01f, hopDuration));
        UpdateHop(t);

        if (t >= 1f)
        {
            transform.position = _hopEnd;
            _inHop = false;
            CheckGoalReached();
        }
    }

    private void ReachGoal()
    {
        if (_state != MovementState.Moving)
            return;

        _state = MovementState.Reached;
        _gameFlow?.SetWin();
    }

    private void OnPathCleared()
    {
        if (_state == MovementState.Idle)
            StartMoving();
    }

    private void StartNextHop()
    {
        var direction = goalTransform.position - transform.position;
        direction.y = 0f;
        var distance = direction.magnitude;
        if (distance <= doorReachDistance)
        {
            CheckGoalReached();
            return;
        }

        var stepDistance = Mathf.Min(moveSpeed * hopDuration, distance);
        if (distance > 0.001f)
            direction /= distance;
        else
            direction = transform.forward;

        _hopStart = transform.position;
        _baseY = _hopStart.y;
        _hopEnd = _hopStart + direction * stepDistance;
        _hopTimer = 0f;
        _inHop = true;
        PlaySquashStretch();
    }

    private void CheckGoalReached()
    {
        if (!HasGoal())
            return;

        var distance = Vector3.Distance(transform.position, goalTransform.position);

        if (distance <= doorReachDistance)
            ReachGoal();
    }

    private void UpdateHop(float t)
    {
        var position = Vector3.Lerp(_hopStart, _hopEnd, t);
        position.y = _baseY + Mathf.Sin(t * Mathf.PI) * jumpHeight;
        transform.position = position;
    }

    private bool HasGoal()
    {
        return goalTransform != null;
    }

    private void PlaySquashStretch()
    {
        if (squashDuration <= 0f || stretchDuration <= 0f)
            return;

        if (_squashSequence != null)
            _squashSequence.Kill();

        var squashScale = new Vector3(
            _defaultScale.x * (2f - squashScaleY),
            _defaultScale.y * squashScaleY,
            _defaultScale.z * (2f - squashScaleY));

        var stretchScale = new Vector3(
            _defaultScale.x * (2f - stretchScaleY),
            _defaultScale.y * stretchScaleY,
            _defaultScale.z * (2f - stretchScaleY));

        _squashSequence = DOTween.Sequence()
            .Append(transform.DOScale(squashScale, squashDuration).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(stretchScale, stretchDuration).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(_defaultScale, stretchDuration).SetEase(Ease.InQuad));
    }

    private void ApplyTuning()
    {
        if (config == null)
            return;

        moveSpeed = config.moveSpeed;
        doorReachDistance = config.doorReachDistance;
        jumpHeight = config.jumpHeight;
        hopDuration = config.hopDuration;
        squashScaleY = config.squashScaleY;
        stretchScaleY = config.stretchScaleY;
        squashDuration = config.squashDuration;
        stretchDuration = config.stretchDuration;
    }

}
