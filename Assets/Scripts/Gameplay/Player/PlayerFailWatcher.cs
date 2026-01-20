using UnityEngine;
using Zenject;

public class PlayerFailWatcher : MonoBehaviour
{
    [Inject] private IPlayerShooting _shooting;
    [Inject] private IObstacleRegistry _levelManager;
    [Inject] private IFailController _failController;
    private bool _waitingForShotCompletion;

    private void OnEnable()
    {
        _shooting.ShotReleased += HandleShotReleased;
        _shooting.ShotCompleted += HandleShotCompleted;

        EvaluatePathClearance();
    }

    private void OnDisable()
    {
        _shooting.ShotReleased -= HandleShotReleased;
        _shooting.ShotCompleted -= HandleShotCompleted;
    }

    private void HandleShotReleased(float shotScale)
    {
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
        if (_levelManager.IsPathCleared)
            return;

        if (_shooting.AvailableScale <= _shooting.MinPlayerScale + 0.001f)
        {
            _failController.TryFail(ResultReason.NotEnoughShotsToClearPath);
        }
    }

}
