using UnityEngine;

[CreateAssetMenu(fileName = "DEF_Global", menuName = "Definitions/Global")]
public class GlobalDefinition : ScriptableObject
{
    public enum ECaughtMode
    {
        TeleportToChair,
        WalkBack,
        TeleportToDoor
    }

    public enum EAnswerSheetMode
    {
        Classic,      // current UI behavior
        SemiCircle    // new world-space semicircle
    }
    public enum EPaperBallType { Normal, LobShot }
    public EPaperBallType CraftedPaperBallType;

    [PlayerCountButtons] public int RequiredPlayerCount = 1;
    public bool CanUseAnyPlayerChair;
    public bool PersistAnswerProgress;
    public bool ShowAnswerSheetOnSit;
    [Range(0f, 1f)]
    public float MinCorrectnessToEarlyVictoryFlow;
    public int PlayerLives;
    public ECaughtMode CaughtMode;
    public EAnswerSheetMode AnswerSheetMode;    

    [Header("TAGS")]
    [Tag] public string PlayerTag;
    [Tag] public string DistractionTag;

    [Header("LAYERS")]
    [Layer] public int ItemLayer;
    [Layer] public int FlyingLayer;
}
