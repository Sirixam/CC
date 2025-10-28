using UnityEngine;

public class PaperBallController : MonoBehaviour
{
    [Tooltip("Use 0 if there's no answer in this paper ball")]
    [SerializeField] private int _answerNumber;

    public bool HasAnswer => _answerNumber > 0;
    public int AnswerNumber => _answerNumber;

    public InteractionController InteractionController => GetComponentInChildren<InteractionController>();

    private void Start()
    {
        if (HasAnswer)
        {
            AnswersManager.GetInstance().OnAllPlayersAnsweredFullyEvent += OnAllPlayersAnsweredFullyEvent;
        }
    }

    private void OnDestroy()
    {
        AnswersManager answersManager = AnswersManager.GetInstance();
        if (answersManager != null)
        {
            answersManager.OnAllPlayersAnsweredFullyEvent -= OnAllPlayersAnsweredFullyEvent;
        }
    }

    public void SetAnswerNumber(int value)
    {
        bool hadAnswer = HasAnswer;
        _answerNumber = value;

        if (hadAnswer != HasAnswer)
        {
            if (hadAnswer)
            {
                AnswersManager.GetInstance().OnAllPlayersAnsweredFullyEvent -= OnAllPlayersAnsweredFullyEvent;
            }
            else
            {
                AnswersManager.GetInstance().OnAllPlayersAnsweredFullyEvent += OnAllPlayersAnsweredFullyEvent;
            }
        }
    }

    private void OnAllPlayersAnsweredFullyEvent(int answerNumber)
    {
        if (_answerNumber != answerNumber) return;
        Destroy(gameObject);
    }
}
