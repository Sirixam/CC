using UnityEngine;

public class DeskController : MonoBehaviour
{
    [SerializeField] private Transform _lookAtPoint;
    [SerializeField] private Transform _sittingPoint;
    [SerializeField] private Transform[] _standingPoints;
    [SerializeField] private AnswersSheetUI _answersSheetUI;

    [SerializeField] private bool _persistProgress;

    [SerializeField] private int _answersCount = 10; // TODO: Move elsewhere
    [SerializeField] private float _answerDuration = 2f; // TODO: Move elsewhere

    private int _activeAnswerNumber;
    private float[] _answersProgress;

    public bool HasAnswers => _answersProgress != null;
    public bool IsAnswering => _activeAnswerNumber > 0;

    public Transform LookAtPoint => _lookAtPoint;
    public Transform SittingPoint => _sittingPoint;
    public Transform[] StandingPoints => _standingPoints;

    private void Awake()
    {
        _answersSheetUI.Hide();
        SetupAnswersSheet(_answersCount);
    }

    public void ShowAnswersSheet()
    {
        if (!HasAnswers) return;
        _answersSheetUI.Show();
    }

    public bool TryStartAnswering(int answerNumber)
    {
        if (!HasAnswers) return false; // No answer sheet in this desk

        float progress = _answersProgress[answerNumber - 1];
        if (progress >= 1) return false; // Already answered

        _activeAnswerNumber = answerNumber;

        _answersSheetUI.ShowProgress(progress);
        _answersSheetUI.Show();
        return true;
    }

    public void UpdateAnswering(out bool finishedAnswering)
    {
        float progressDelta = Time.deltaTime / _answerDuration;
        int answerIndex = _activeAnswerNumber - 1;
        _answersProgress[answerIndex] = Mathf.Clamp01(_answersProgress[answerIndex] + progressDelta);
        finishedAnswering = _answersProgress[answerIndex] >= 1;
        _answersSheetUI.SetProgress(_answersProgress[answerIndex]);
        if (finishedAnswering)
        {
            _answersSheetUI.SetAnswerState(answerIndex, true);
            _answersSheetUI.HideProgress();
            _activeAnswerNumber = 0;
        }
    }

    public void HideAnswersSheet()
    {
        if (!HasAnswers) return;
        _answersSheetUI.Hide();

        if (IsAnswering)
        {
            _answersSheetUI.HideProgress();
            if (!_persistProgress)
            {
                _answersProgress[_activeAnswerNumber - 1] = 0;
            }
            _activeAnswerNumber = 0;
        }
    }

    public void SetupAnswersSheet(int answersCount)
    {
        _answersProgress = new float[answersCount];
        _answersSheetUI.Setup(answersCount);
    }
}
