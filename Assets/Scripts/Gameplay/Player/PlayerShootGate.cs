using UnityEngine;
using Zenject;

public class PlayerShootGate : MonoBehaviour
{
    [Inject] private IPlayerShooting _shooting;
    [Inject] private IObstacleRegistry _levelManager;

    private void OnEnable()
    {
        _levelManager.PathCleared += DisableShooting;
    }

    private void OnDisable()
    {
        _levelManager.PathCleared -= DisableShooting;
    }

    private void DisableShooting()
    {
        _shooting.SetShootingEnabled(false);
    }

}
