using UnityEngine;

[CreateAssetMenu(fileName = "DEF_Global", menuName = "Definitions/Global")]
public class GlobalDefinition : ScriptableObject
{
    public bool StartGameWhenAllPlayersJoined;
    public bool CanUseAnyPlayerChair;
    public bool PersistAnswerProgress;
    public bool SimulateStudentsIndividually;
    [Range(0f, 1f)]
    public float MinCorrectnessToEarlyVictoryFlow;
    public int PlayerLives;
    public Vector2 PreAnsweringDelay;
    public Vector2 AnsweringDelay;
    public Vector2 PostAnsweringDelay;

    [Header("TAGS")]
    [Tag] public string PlayerTag;
    [Tag] public string DistractionTag;

    [Header("LAYERS")]
    [Layer] public int ItemLayer;
    [Layer] public int FlyingLayer;
}
