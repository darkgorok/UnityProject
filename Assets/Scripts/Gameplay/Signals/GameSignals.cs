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
