using UnityEngine;
using Zenject;

public class PlayerShootGate : MonoBehaviour
{
    [Inject] private PlayerShooting _shooting;
    [Inject] private IObstacleRegistry _levelManager;
    [Inject(Optional = true)] private Zenject.SignalBus _signalBus;

    private void Start()
    {
        if (_levelManager == null)
            return;

    }

    private void OnDestroy()
    {
    }

    private void OnEnable()
    {
        if (_signalBus != null)
            _signalBus.Subscribe<PathClearedSignal>(DisableShooting);
    }

    private void OnDisable()
    {
        if (_signalBus != null)
            _signalBus.TryUnsubscribe<PathClearedSignal>(DisableShooting);
    }

    private void DisableShooting()
    {
        _shooting?.SetShootingEnabled(false);
    }

}
