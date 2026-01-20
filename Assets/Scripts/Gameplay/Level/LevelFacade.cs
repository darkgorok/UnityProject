using UnityEngine;

public sealed class LevelFacade : MonoBehaviour
{
    [SerializeField] private DoorController door;
    [SerializeField] private GoalAimDirectionProvider aimProvider;

    public DoorController Door => door;
    public GoalAimDirectionProvider AimProvider => aimProvider;
}
