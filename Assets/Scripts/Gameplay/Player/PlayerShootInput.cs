using UnityEngine;
using Zenject;

public class PlayerShootInput : MonoBehaviour, ITickable
{
    private enum InputState
    {
        Idle,
        Charging
    }

    [Inject] private PlayerShooting _shooting;
    [Inject] private ITimeProvider _timeProvider;
    [Inject(Optional = true)] private IInputService _inputService;

    private InputState _state = InputState.Idle;

    public void Tick()
    {
        if (_shooting == null)
            return;

        if (_state == InputState.Charging && !_shooting.IsCharging)
            _state = InputState.Idle;

        switch (_state)
        {
            case InputState.Idle:
                HandleIdle();
                break;

            case InputState.Charging:
                HandleCharging();
                break;
        }
    }

    private bool GetPressed()
    {
        return _inputService != null
            ? _inputService.GetMouseButtonDown(0)
            : Input.GetMouseButtonDown(0);
    }

    private bool GetReleased()
    {
        return _inputService != null
            ? _inputService.GetMouseButtonUp(0)
            : Input.GetMouseButtonUp(0);
    }

    private void HandleIdle()
    {
        if (GetPressed() && _shooting.BeginCharge())
            _state = InputState.Charging;

        if (GetReleased())
            _shooting.CancelCharge();
    }

    private void HandleCharging()
    {
        _shooting.TickCharge(_timeProvider.DeltaTime);
        if (GetReleased())
        {
            _shooting.ReleaseShot();
            _state = InputState.Idle;
        }
    }

}
