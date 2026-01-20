using UnityEngine;

public class UnityTimeProvider : ITimeProvider
{
    public float DeltaTime => Time.deltaTime;
}
