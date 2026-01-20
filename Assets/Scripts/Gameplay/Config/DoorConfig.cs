using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Door Config", fileName = "DoorConfig")]
public class DoorConfig : ScriptableObject
{
    [Header("Trigger")]
    [Tooltip("Trigger radius/size for door opening.")]
    public float openDistance = 5f;
    [Header("Animation")]
    [Tooltip("Door rotation angle on open (degrees).")]
    public float openAngle = 90f;
    [Tooltip("Seconds to open door.")]
    public float openDuration = 0.5f;
    [Tooltip("Easing for door opening.")]
    public DG.Tweening.Ease openEase = DG.Tweening.Ease.OutQuad;
}
