using UnityEngine;
using Zenject;

public class PlayerFailWatcher : MonoBehaviour
{
    [Inject] private PlayerShooting _shooting;
    [Inject] private IObstacleRegistry _levelManager;
    [Inject(Optional = true)] private IFailController _failController;
    private bool _waitingForShotCompletion;

    private void OnEnable()
    {
        if (_shooting != null)
        {
            _shooting.ShotReleased += HandleShotReleased;
            _shooting.ShotCompleted += HandleShotCompleted;
        }

        EvaluatePathClearance();
    }

    private void OnDisable()
    {
        if (_shooting != null)
        {
            _shooting.ShotReleased -= HandleShotReleased;
            _shooting.ShotCompleted -= HandleShotCompleted;
        }
    }

    private void HandleShotReleased(float shotScale)
    {
        if (_shooting == null)
            return;

        if (_shooting.HasActiveProjectile)
        {
            _waitingForShotCompletion = true;
            return;
        }

        EvaluatePathClearance();
    }

    private void HandleShotCompleted()
    {
        if (_waitingForShotCompletion)
            _waitingForShotCompletion = false;
        EvaluatePathClearance();
    }

    private void EvaluatePathClearance()
    {
        if (_shooting == null || _levelManager == null)
            return;

        if (_levelManager.IsPathCleared)
            return;

        if (_shooting.AvailableScale <= _shooting.MinPlayerScale + 0.001f)
        {
            if (_failController != null)
                _failController.TryFail(ResultReason.NotEnoughShotsToClearPath);
            else
                _shooting.TriggerFailure(ResultReason.NotEnoughShotsToClearPath);
        }
    }

}
