using UnityEngine;

public class PaperBallController : MonoBehaviour
{
    [Tooltip("Use 0 if there's no answer in this paper ball")]
    [SerializeField] private int _answerNumber;

    public bool HasAnswer => _answerNumber > 0;
    public int AnswerNumber => _answerNumber;
}
