using System;

public interface IDoor
{
    event Action Opened;
    bool IsOpen { get; }
    void Open();
}
