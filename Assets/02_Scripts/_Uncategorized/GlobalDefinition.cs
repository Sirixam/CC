using UnityEngine;

[CreateAssetMenu(fileName = "DEF_Global", menuName = "Definitions/Global")]
public class GlobalDefinition : ScriptableObject
{
    public bool StartGameWhenAllPlayersJoined;
    public bool CanUseAnyPlayerChair;
    public bool PersistAnswerProgress;
    public bool SimulateStudentsIndividually;
    public bool ShowAnswerSheetOnSit;
    [Range(0f, 1f)]
    public float MinCorrectnessToEarlyVictoryFlow;
    public int PlayerLives;
    public Vector2 PreAnsweringDelay;
    public Vector2 AnsweringDuration;
    public Vector2 PostAnsweringDelay;
    public enum ECaughtMode { Teleport, WalkBack }
    public ECaughtMode CaughtMode;
    public enum EAnswerSheetMode
    {
        Classic,      // current UI behavior
        SemiCircle    // new world-space semicircle
    }
    public EAnswerSheetMode AnswerSheetMode;

    [Header("TAGS")]
    [Tag] public string PlayerTag;
    [Tag] public string DistractionTag;

    [Header("LAYERS")]
    [Layer] public int ItemLayer;
    [Layer] public int FlyingLayer;
}
