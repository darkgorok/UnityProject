public sealed class CooldownController
{
    private float _cooldownTimer;

    public bool IsActive => _cooldownTimer > 0f;

    public void Start(float duration)
    {
        _cooldownTimer = duration;
    }

    public bool Tick(float deltaTime)
    {
        if (_cooldownTimer <= 0f)
            return false;

        _cooldownTimer -= deltaTime;
        if (_cooldownTimer <= 0f)
        {
            _cooldownTimer = 0f;
            return true;
        }

        return false;
    }
}
