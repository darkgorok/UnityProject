using UnityEngine;
using Zenject;

public class PlayerShootGate : MonoBehaviour
{
    [Inject] private PlayerShooting _shooting;
    [Inject] private IObstacleRegistry _levelManager;

    private void OnEnable()
    {
        if (_levelManager != null)
            _levelManager.PathCleared += DisableShooting;
    }

    private void OnDisable()
    {
        if (_levelManager != null)
            _levelManager.PathCleared -= DisableShooting;
    }

    private void DisableShooting()
    {
        _shooting?.SetShootingEnabled(false);
    }

}
