using UnityEngine;

[CreateAssetMenu(fileName = "DEF_Answer_", menuName = "Definitions/Answer")]
public class AnswerDefinition : ScriptableObject
{
    public const float PERFECT_CORRECTNESS = 1f;

    public string ID;
    public Sprite Icon;
    public Color Color = Color.white;
    public float BaseAnswerDuration;
}
