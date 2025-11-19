using Cysharp.Threading.Tasks;
using System.Collections.Generic;
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
    private CancellationTokenSource _gameCancellationSource;

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

    // Triggere externallyd when a player joins the game
    public void OnPlayerJoined()
    {
        StartGame();
    }

    private void StartGame()
    {
        _gameCancellationSource = new CancellationTokenSource();
        StartTimer();
        _answerManager.StartStimulation(_gameCancellationSource.Token);
    }

    private void StopGame()
    {
        _gameCancellationSource.Cancel();
    }

    private void StartTimer()
    {
        _timeHelper.Setup(_maxTimeInSeconds);
        _timeHelper.StartTimer(_gameCancellationSource.Token).Forget();
    }

    private void OnAllPlayersFinishedAllAnswers()
    {
        StopGame();
        if (_victoryFeedback != null)
        {
            _victoryFeedback.SetActive(true);
        }
    }

    private void OnTimesUp()
    {
        StopGame();
        if (_timesUpFeedback != null)
        {
            _timesUpFeedback.SetActive(true);
        }
    }
}
