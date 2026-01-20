using UnityEngine;
using Zenject;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class DoorController : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float openDistance = 5f;
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private Rigidbody doorRigidbody;
    [SerializeField] private DoorConfig config;

    [Inject] private IDoor _door;
    [Inject(Optional = true, Id = "Player")] private Transform _injectedPlayer;

    private void Awake()
    {
        ApplyConfig();
        if (playerTransform == null)
            playerTransform = _injectedPlayer;

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

    private void OnTriggerEnter(Collider other)
    {
        if (_door.IsOpen)
            return;

        var isPlayer = playerTransform != null
            ? other.transform == playerTransform
            : other.CompareTag("Player");
        if (!isPlayer)
            return;

        _door.Open();
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
            triggerCollider = GetComponent<Collider>();
        if (doorRigidbody == null)
            doorRigidbody = GetComponent<Rigidbody>();
    }
#endif
}
