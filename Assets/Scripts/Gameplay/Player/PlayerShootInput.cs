using UnityEngine;
using Zenject;

public class PlayerShootInput : MonoBehaviour, ITickable
{
    private enum InputState
    {
        Idle,
        Charging
    }

    [SerializeField] private Camera inputCamera;
    [SerializeField] private float rayDistance = 100f;

    [Inject] private PlayerShooting _shooting;
    [Inject(Optional = true)] private Camera _injectedCamera;
    [Inject(Optional = true)] private ITimeProvider _timeProvider;

    private InputState _state = InputState.Idle;

    private void Awake()
    {
        if (inputCamera == null)
            inputCamera = _injectedCamera;
    }

    public void Tick()
    {
        if (_shooting == null)
            return;

        if (_state == InputState.Charging && !_shooting.IsCharging)
            _state = InputState.Idle;

        var pressed = Input.GetMouseButtonDown(0);
        var released = Input.GetMouseButtonUp(0);

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

    private bool IsPointerOverPlayer()
    {
        return true;
    }
}
