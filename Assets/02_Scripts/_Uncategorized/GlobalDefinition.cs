using UnityEngine;

[CreateAssetMenu(fileName = "DEF_Global", menuName = "Definitions/Global")]
public class GlobalDefinition : ScriptableObject
{
    public bool StartGameWhenAllPlayersJoined;
    public bool CanUseAnyPlayerChair;
    public bool PersistAnswerProgress;
    public float PeekMaxShowDuration;
    public int PlayerLives;
    public Vector2 PreAnsweringDelay;
    public Vector2 PostAnsweringDelay;
}
