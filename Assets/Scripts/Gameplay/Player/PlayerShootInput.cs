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
    [Inject(Optional = true)] private ITimeProvider _timeProvider;
    [Inject(Optional = true)] private IInputService _inputService;

    private InputState _state = InputState.Idle;

    public void Tick()
    {
        if (_shooting == null)
            return;

        if (_state == InputState.Charging && !_shooting.IsCharging)
            _state = InputState.Idle;

        var pressed = _inputService != null
            ? _inputService.GetMouseButtonDown(0)
            : Input.GetMouseButtonDown(0);
        var released = _inputService != null
            ? _inputService.GetMouseButtonUp(0)
            : Input.GetMouseButtonUp(0);

        switch (_state)
        {
            case InputState.Idle:
                if (pressed && _shooting.BeginCharge())
                {
                    _state = InputState.Charging;
                }
                if (released)
                    _shooting.CancelCharge();
                break;

            case InputState.Charging:
                _shooting.TickCharge(_timeProvider != null ? _timeProvider.DeltaTime : Time.deltaTime);
                if (released)
                {
                    _shooting.ReleaseShot();
                    _state = InputState.Idle;
                }
                break;
        }
    }

}
