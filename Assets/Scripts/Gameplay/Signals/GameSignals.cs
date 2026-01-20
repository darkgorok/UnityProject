public sealed class GameStartSignal
{
}

public sealed class ShotReleasedSignal
{
    public ShotReleasedSignal(float shotScale)
    {
        ShotScale = shotScale;
    }

    public float ShotScale { get; }
}

public sealed class ShotCompletedSignal
{
}

public sealed class PathClearedSignal
{
}

public sealed class WinSignal
{
}

public sealed class LoseSignal
{
    public LoseSignal(ResultReason reason, string detail = null)
    {
        Reason = reason;
        Detail = detail;
    }

    public ResultReason Reason { get; }
    public string Detail { get; }
}
