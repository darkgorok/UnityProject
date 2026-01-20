using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Player Movement Config", fileName = "PlayerMovementConfig")]
public class PlayerMovementConfig : ScriptableObject
{
    public float moveSpeed = 4f;
    public float doorReachDistance = 1f;
    public float jumpHeight = 0.6f;
    public float hopDuration = 0.4f;
    public float squashScaleY = 0.75f;
    public float stretchScaleY = 1.2f;
    public float squashDuration = 0.08f;
    public float stretchDuration = 0.1f;
}
