using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour, IProjectile
{
    public class Pool : Zenject.MonoMemoryPool<Projectile>
    {
        protected override void OnSpawned(Projectile item)
        {
            item.SetReleaseAction(Despawn);
            item.gameObject.SetActive(true);
        }

        protected override void OnDespawned(Projectile item)
        {
            item.ResetState();
            item.gameObject.SetActive(false);
            item.SetReleaseAction(null);
        }
    }

    [SerializeField] private Rigidbody projectileRigidbody;
    [SerializeField] private Collider projectileCollider;
    private float _infectionRadius;
    private bool _hasExploded;
    private System.Action<Projectile> _releaseAction;
    private Coroutine _lifeRoutine;
    private Collider[] _overlapResults;

    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private int overlapBufferSize = 16;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField] private MonoBehaviour infectionVfxSource;
    [SerializeField] private ProjectileConfig config;

    private IProjectileVfx _infectionVfx;
    [Zenject.Inject(Optional = true)] private IObstacleResolver _obstacleResolver;

    public event Action Completed;
    public Collider Collider => projectileCollider;

    private void Awake()
    {
        ApplyTuning();
        _overlapResults = new Collider[Mathf.Max(1, overlapBufferSize)];
        _infectionVfx = infectionVfxSource as IProjectileVfx;
    }

    public void SetReleaseAction(System.Action<Projectile> releaseAction)
    {
        _releaseAction = releaseAction;
    }

    public void Initialize(Vector3 direction, float speed, float size, float infectionRadius)
    {
        _hasExploded = false;
        transform.localScale = Vector3.one * size;
        _infectionRadius = infectionRadius;

        projectileRigidbody.useGravity = false;
        projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        projectileRigidbody.velocity = direction.normalized * speed;

        projectileCollider.enabled = true;

        if (_lifeRoutine != null)
            StopCoroutine(_lifeRoutine);
        _lifeRoutine = StartCoroutine(LifeTimer());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasExploded)
            return;

        if (_obstacleResolver != null &&
            _obstacleResolver.TryGetObstacle(collision.collider, out _))
        {
            InfectArea(collision.contacts[0].point);
            return;
        }

        _hasExploded = true;
        ReleaseSelf();
    }

    private void InfectArea(Vector3 center)
    {
        if (_lifeRoutine != null)
        {
            StopCoroutine(_lifeRoutine);
            _lifeRoutine = null;
        }

        _hasExploded = true;

        _infectionVfx?.PlayInfection(center, _infectionRadius);

        var hitsCount = Physics.OverlapSphereNonAlloc(
            center,
            _infectionRadius,
            _overlapResults,
            obstacleLayers,
            QueryTriggerInteraction.Collide);

        for (var i = 0; i < hitsCount; i++)
        {
            var hit = _overlapResults[i];
            if (hit == null)
                continue;

            if (_obstacleResolver != null &&
                _obstacleResolver.TryGetObstacle(hit, out var obstacle))
            {
                obstacle.Infect();
            }
        }

        ReleaseSelf();
    }

    public void ResetState()
    {
        if (_lifeRoutine != null)
        {
            StopCoroutine(_lifeRoutine);
            _lifeRoutine = null;
        }

        _hasExploded = false;
        if (projectileCollider != null)
            projectileCollider.enabled = true;

        projectileRigidbody.velocity = Vector3.zero;
        projectileRigidbody.angularVelocity = Vector3.zero;
    }

    private System.Collections.IEnumerator LifeTimer()
    {
        if (lifeTime > 0f)
            yield return new WaitForSeconds(lifeTime);

        if (!_hasExploded)
            ReleaseSelf();
    }

    private void ReleaseSelf()
    {
        if (!_hasExploded)
            _hasExploded = true;

        Completed?.Invoke();

        if (_releaseAction != null)
            _releaseAction(this);
        else
            Destroy(gameObject);
    }

    private void ApplyTuning()
    {
        if (config == null)
            return;

        lifeTime = config.lifeTime;
        overlapBufferSize = config.overlapBufferSize;
        obstacleLayers = config.obstacleLayers;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (projectileRigidbody == null)
            projectileRigidbody = GetComponent<Rigidbody>();
        if (projectileCollider == null)
            projectileCollider = GetComponent<Collider>();
    }
#endif
}
