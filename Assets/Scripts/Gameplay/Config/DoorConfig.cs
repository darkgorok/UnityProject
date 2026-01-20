using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Door Config", fileName = "DoorConfig")]
public class DoorConfig : ScriptableObject
{
    [Header("Trigger")]
    public float openDistance = 5f;
}
