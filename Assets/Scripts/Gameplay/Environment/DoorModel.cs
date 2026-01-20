using System;

public class DoorModel : IDoor
{
    public event Action Opened;
    public bool IsOpen { get; private set; }

    public void Open()
    {
        if (IsOpen)
            return;

        IsOpen = true;
        Opened?.Invoke();
    }
}
