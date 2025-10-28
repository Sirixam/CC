using UnityEngine;

public class PaperBallController : MonoBehaviour
{
    [Tooltip("Use 0 if there's no answer in this paper ball")]
    [SerializeField] private int _answerNumber;

    public bool HasAnswer => _answerNumber > 0;
    public int AnswerNumber => _answerNumber;

    public InteractionController InteractionController => GetComponentInChildren<InteractionController>();

    public void SetAnswerNumber(int value)
    {
        _answerNumber = value;
    }
}
