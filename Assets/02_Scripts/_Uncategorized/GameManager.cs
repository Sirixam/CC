using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private AnswersManager _answerManager;

    [SerializeField] private GameObject _victoryFeedback;
    [SerializeField] private TimeUI _timeUI;
    [SerializeField] private GameObject _timesUpFeedback;
    [SerializeField] private ButtonListener[] _restartButtons;

    [Header("Configuratinos")]
    [SerializeField] private GlobalDefinition _globalDefinition;
    [SerializeField] private float _maxTimeInSeconds = 30;

    private int _playersCount;
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
        foreach (var button in _restartButtons)
        {
            button.OnClickEvent += RestartGame;
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
        if (_gameCancellationSource != null) return; // Game already started

        if (++_playersCount >= _answerManager.RequiredPlayersCount || !_globalDefinition.StartGameWhenAllPlayersJoined)
        {
            StartGame();
        }
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

    private void RestartGame()
    {
        _answerManager.ResetProgress();
        if (_victoryFeedback != null)
        {
            _victoryFeedback.SetActive(false);
        }
        if (_timesUpFeedback != null)
        {
            _timesUpFeedback.SetActive(false);
        }
        StopGame();
        StartGame();
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
