using UnityEngine;

[CreateAssetMenu(menuName = "AlphaTest/Config/Level Flow Config", fileName = "LevelFlowConfig")]
public sealed class LevelFlowConfig : ScriptableObject
{
    public bool allowEmptyPathStart = false;
}
