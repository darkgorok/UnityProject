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
    [System.Serializable]
    private struct JumpSettings
    {
        public float moveSpeed;
        public float doorReachDistance;
        public float jumpHeight;
        public float hopDuration;
        public float squashScaleY;
        public float stretchScaleY;
        public float squashDuration;
        public float stretchDuration;
    }
    [Header("References")]
    [SerializeField] private Transform goalTransform;
    [SerializeField] private PlayerMovementConfig config;

    [Header("Jump Settings")]
    [SerializeField] private JumpSettings jump = new JumpSettings
    {
        moveSpeed = 4f,
        doorReachDistance = 1.5f,
        jumpHeight = 0.6f,
        hopDuration = 0.4f,
        squashScaleY = 0.75f,
        stretchScaleY = 1.2f,
        squashDuration = 0.08f,
        stretchDuration = 0.1f
    };

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
        var t = Mathf.Clamp01(_hopTimer / Mathf.Max(0.01f, jump.hopDuration));
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
        if (distance <= jump.doorReachDistance)
        {
            CheckGoalReached();
            return;
        }

        var stepDistance = Mathf.Min(jump.moveSpeed * jump.hopDuration, distance);
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

        if (distance <= jump.doorReachDistance)
            ReachGoal();
    }

    private void UpdateHop(float t)
    {
        var position = Vector3.Lerp(_hopStart, _hopEnd, t);
        position.y = _baseY + Mathf.Sin(t * Mathf.PI) * jump.jumpHeight;
        transform.position = position;
    }

    private bool HasGoal()
    {
        return goalTransform != null;
    }

    private void PlaySquashStretch()
    {
        if (jump.squashDuration <= 0f || jump.stretchDuration <= 0f)
            return;

        if (_squashSequence != null)
            _squashSequence.Kill();

        var squashScale = new Vector3(
            _defaultScale.x * (2f - jump.squashScaleY),
            _defaultScale.y * jump.squashScaleY,
            _defaultScale.z * (2f - jump.squashScaleY));

        var stretchScale = new Vector3(
            _defaultScale.x * (2f - jump.stretchScaleY),
            _defaultScale.y * jump.stretchScaleY,
            _defaultScale.z * (2f - jump.stretchScaleY));

        _squashSequence = DOTween.Sequence()
            .Append(transform.DOScale(squashScale, jump.squashDuration).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(stretchScale, jump.stretchDuration).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(_defaultScale, jump.stretchDuration).SetEase(Ease.InQuad));
    }

    private void ApplyTuning()
    {
        if (config == null)
            return;

        jump.moveSpeed = config.moveSpeed;
        jump.doorReachDistance = config.doorReachDistance;
        jump.jumpHeight = config.jumpHeight;
        jump.hopDuration = config.hopDuration;
        jump.squashScaleY = config.squashScaleY;
        jump.stretchScaleY = config.stretchScaleY;
        jump.squashDuration = config.squashDuration;
        jump.stretchDuration = config.stretchDuration;
    }

}
