using System;

public class ObstacleModel : IObstacle
{
    public event Action Infected;
    public bool IsInfected { get; private set; }

    public void Infect()
    {
        if (IsInfected)
            return;

        IsInfected = true;
        Infected?.Invoke();
    }
}
