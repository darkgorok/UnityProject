using UnityEngine;
using Zenject;

[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 0.1f;
    [SerializeField] private Collider obstacleCollider;

    private IObstacleRegistry _registry;
    private IObstacleResolver _resolver;

    private ObstacleModel _model;
    private bool _isInjected;
    private bool _isRegistered;

    private void Awake()
    {
        _model = new ObstacleModel();
    }

    private void OnEnable()
    {
        TryRegister();
    }

    private void Start()
    {
        TryRegister();
    }

    private void OnDisable()
    {
        if (_isRegistered)
        {
            _resolver?.Unregister(obstacleCollider);
            _model.Infected -= HandleInfected;
            _isRegistered = false;
        }
    }

    private void HandleInfected()
    {
        _registry?.NotifyObstacleCleared(_model);
        Destroy(gameObject, destroyDelay);
    }

    [Inject]
    private void Construct(IObstacleRegistry registry, IObstacleResolver resolver)
    {
        _registry = registry;
        _resolver = resolver;
        _isInjected = true;
    }

    private void TryRegister()
    {
        if (_isRegistered || !_isInjected)
            return;

        if (obstacleCollider == null)
        {
            Debug.LogError("Obstacle collider reference is missing.", this);
            enabled = false;
            return;
        }

        _model.Infected += HandleInfected;
        _registry?.Register(_model);
        _resolver?.Register(obstacleCollider, _model);
        _isRegistered = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (obstacleCollider == null)
            obstacleCollider = GetComponent<Collider>();
    }
#endif
}
