using UnityEngine;
using Zenject;
using DG.Tweening;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class DoorController : MonoBehaviour
{
    [SerializeField] private float openDistance = 5f;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openDuration = 0.5f;
    [SerializeField] private Ease openEase = Ease.OutQuad;
    [SerializeField] private Transform doorPivot;
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private Rigidbody doorRigidbody;
    [SerializeField] private DoorConfig config;

    [Inject] private IDoor _door;
    private Quaternion _closedRotation;
    private Tween _openTween;

    private void Awake()
    {
        ApplyConfig();
        if (triggerCollider == null)
            triggerCollider = GetComponentInChildren<Collider>();
        if (doorRigidbody == null)
            doorRigidbody = GetComponent<Rigidbody>();

        if (doorPivot == null)
            doorPivot = transform;

        _closedRotation = doorPivot.localRotation;

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            if (triggerCollider is SphereCollider sphereCollider)
                sphereCollider.radius = openDistance;
        }
        if (doorRigidbody != null)
        {
            doorRigidbody.isKinematic = true;
            doorRigidbody.useGravity = false;
        }
    }

    private void OnEnable()
    {
        if (_door != null)
        {
            _door.Opened += HandleDoorOpened;
            if (_door.IsOpen)
                HandleDoorOpened();
        }
    }

    private void OnDisable()
    {
        if (_door != null)
            _door.Opened -= HandleDoorOpened;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_door.IsOpen)
            return;

        if (!IsPlayerTrigger(other))
            return;

        _door.Open();
    }

    private void HandleDoorOpened()
    {
        if (doorPivot == null)
            return;

        if (_openTween != null)
            _openTween.Kill();

        var targetRotation = _closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        _openTween = doorPivot.DOLocalRotateQuaternion(targetRotation, openDuration)
            .SetEase(openEase)
            .SetLink(doorPivot.gameObject, LinkBehaviour.KillOnDisable);
    }

    private static bool IsPlayerTrigger(Collider other)
    {
        return other.TryGetComponent<PlayerMarker>(out _)
            || other.GetComponentInParent<PlayerMarker>() != null;
    }

    private void ApplyConfig()
    {
        if (config == null)
            return;

        openDistance = config.openDistance;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponentInChildren<Collider>();
        if (doorRigidbody == null)
            doorRigidbody = GetComponent<Rigidbody>();
    }
#endif
}
