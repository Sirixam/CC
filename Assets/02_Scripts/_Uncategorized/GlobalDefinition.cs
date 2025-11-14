using UnityEngine;

[CreateAssetMenu(fileName = "DEF_Global", menuName = "Definitions/Global")]
public class GlobalDefinition : ScriptableObject
{
    public bool CanUseAnyPlayerChair;
    public bool PersistAnswerProgress;
    public float PostAnsweringDelayMin;
    public float PostAnsweringDelayMax;
}
