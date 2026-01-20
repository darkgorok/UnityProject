using UnityEngine;

public sealed class MobileInputService : IInputService
{
    public bool GetMouseButtonDown(int button)
    {
        if (button != 0 || Input.touchCount == 0)
            return false;

        return Input.GetTouch(0).phase == TouchPhase.Began;
    }

    public bool GetMouseButtonUp(int button)
    {
        if (button != 0 || Input.touchCount == 0)
            return false;

        return Input.GetTouch(0).phase == TouchPhase.Ended
            || Input.GetTouch(0).phase == TouchPhase.Canceled;
    }
}
