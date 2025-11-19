using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private AnswersManager _answerManager;

    [SerializeField] private GameObject _victoryFeedback;
    [SerializeField] private TimeUI _timeUI;
    [SerializeField] private GameObject _timesUpFeedback;

    [Header("Configuratinos")]
    [SerializeField] private float _maxTimeInSeconds = 30;

    private TimeHelper _timeHelper;
    private CancellationToken _timeCancellationToken;

    private void Awake()
    {
        if (_victoryFeedback != null)
        {
            _victoryFeedback.SetActive(false);
        }
        if (_timesUpFeedback != null)
        {
            _timesUpFeedback.SetActive(false);
        }
        if (_timeUI != null)
        {
            _timeUI.gameObject.SetActive(false);
        }
        _timeHelper = new TimeHelper(_timeUI);
    }

    private void OnEnable()
    {
        _timeHelper.OnTimesUp += OnTimesUp;
        _answerManager.OnAllPlayersFinishedAllAnswers += OnAllPlayersFinishedAllAnswers;
    }

    private void OnDisable()
    {
        _timeHelper.OnTimesUp -= OnTimesUp;
        _answerManager.OnAllPlayersFinishedAllAnswers -= OnAllPlayersFinishedAllAnswers;
    }

    private void Start()
    {
        _timeHelper.Setup(_maxTimeInSeconds);
        _timeCancellationToken = new CancellationToken();
        _timeHelper.StartTimer(_timeCancellationToken).Forget();
    }

    private void OnAllPlayersFinishedAllAnswers()
    {
        _timeHelper.Pause();
        if (_victoryFeedback != null)
        {
            _victoryFeedback.SetActive(true);
        }
    }

    private void OnTimesUp()
    {
        if (_timesUpFeedback != null)
        {
            _timesUpFeedback.SetActive(true);
        }
    }
}
