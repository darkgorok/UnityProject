using System;

public interface IPlayerShooting
{
    float AvailableScale { get; }
    float MinPlayerScale { get; }
    bool IsCharging { get; }
    bool HasActiveProjectile { get; }
    event Action<float> ShotReleased;
    event Action ShotCompleted;
    bool BeginCharge();
    void TickCharge(float deltaTime);
    void ReleaseShot();
    void CancelCharge();
    void TriggerFailure(ResultReason reason);
    void SetShootingEnabled(bool enabled);
}
