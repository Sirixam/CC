using UnityEngine;

public class PaperBallController : MonoBehaviour
{
    [Tooltip("Use 0 if there's no answer in this paper ball")]
    [SerializeField] private AnswerDefinition _defaultAnswerDefinition;

    private string _answerID;

    public bool HasAnswer => !string.IsNullOrWhiteSpace(_answerID) || _defaultAnswerDefinition != null;
    public string AnswerID => !string.IsNullOrWhiteSpace(_answerID) ? _answerID : _defaultAnswerDefinition != null ? _defaultAnswerDefinition.ID : null;

    public InteractionController InteractionController => GetComponentInChildren<InteractionController>();

    private void Start()
    {
        if (HasAnswer)
        {
            AnswersManager.GetInstance().OnAllPlayersFinishedAnswer += OnAllPlayersAnsweredFullyEvent;
        }
    }

    private void OnDestroy()
    {
        AnswersManager answersManager = AnswersManager.GetInstance();
        if (answersManager != null)
        {
            answersManager.OnAllPlayersFinishedAnswer -= OnAllPlayersAnsweredFullyEvent;
        }
    }

    public void SetAnswer(string answerID)
    {
        bool hadAnswer = HasAnswer;
        _answerID = answerID;

        if (hadAnswer != HasAnswer)
        {
            if (hadAnswer)
            {
                AnswersManager.GetInstance().OnAllPlayersFinishedAnswer -= OnAllPlayersAnsweredFullyEvent;
            }
            else
            {
                AnswersManager.GetInstance().OnAllPlayersFinishedAnswer += OnAllPlayersAnsweredFullyEvent;
            }
        }
    }

    private void OnAllPlayersAnsweredFullyEvent(string answerID)
    {
        if (AnswerID != answerID) return;
        Destroy(gameObject);
    }
}
