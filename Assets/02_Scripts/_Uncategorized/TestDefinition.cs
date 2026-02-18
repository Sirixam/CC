using UnityEngine;

[CreateAssetMenu(fileName = "DEF_Global_Test", menuName = "Definitions/Global")]
public class TestDefinition : ScriptableObject
{
    public enum ECorrectness
    {
        Undefined,
        Wrong,
        HalfCorrect,
        Correct
    }

    [Header("Answer")]
    [SerializeField] private ECorrectness _correctness;
    public AnswerDefinition ForcedNpcAnswer;

    public int ForcedDistractionLevel;

    public bool ForcedHalfCorrectAnswer => _correctness == ECorrectness.HalfCorrect;
    public bool ForcedWrongAnswer => _correctness == ECorrectness.Wrong;
    public bool ForcedCorrectAnswer => _correctness == ECorrectness.Correct;
}
