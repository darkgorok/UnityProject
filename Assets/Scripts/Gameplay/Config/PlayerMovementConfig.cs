using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Player Movement Config", fileName = "PlayerMovementConfig")]
public class PlayerMovementConfig : ScriptableObject
{
    [Header("Movement")]
    [Tooltip("Horizontal movement speed.")]
    public float moveSpeed = 4f;
    [Tooltip("Distance to goal required to win.")]
    public float doorReachDistance = 1.5f;
    [Tooltip("Jump arc height.")]
    public float jumpHeight = 0.6f;
    [Tooltip("Seconds per hop.")]
    public float hopDuration = 0.4f;
    [Header("Jump Squash")]
    [Tooltip("Y scale multiplier on squash.")]
    public float squashScaleY = 0.75f;
    [Tooltip("Y scale multiplier on stretch.")]
    public float stretchScaleY = 1.2f;
    [Tooltip("Seconds for squash.")]
    public float squashDuration = 0.08f;
    [Tooltip("Seconds for stretch.")]
    public float stretchDuration = 0.1f;
}
