using UnityEngine;

public class GoalAimDirectionProvider : MonoBehaviour, IAimDirectionProvider
{
    [SerializeField] private Transform target;

    public Vector3 GetDirection(Vector3 origin)
    {
        if (target == null)
            return transform.forward;

        var direction = target.position - origin;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }
}
