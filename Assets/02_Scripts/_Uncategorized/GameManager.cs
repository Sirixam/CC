using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private AnswersManager _answerManager;
    [SerializeField] private StudentManager _studentManager;

    [SerializeField] private GameObject _defeatFeedback;
    [SerializeField] private GameObject _victoryFeedback;
    [SerializeField] private TimeUI _timeUI;
    [SerializeField] private LivesUI _livesUI;
    [SerializeField] private GameObject _timesUpFeedback;
    [SerializeField] private ButtonListener[] _restartButtons;

    [Header("Configuratinos")]
    [SerializeField] private GlobalDefinition _globalDefinition;
    [SerializeField] private float _maxTimeInSeconds = 30;

    private TimeHelper _timeHelper;
    private CancellationTokenSource _gameCancellationSource;
    private List<PlayerController> _players = new();
    private int _playerLives;

    private void Awake()
    {
        if (_defeatFeedback != null)
        {
            _defeatFeedback.SetActive(false);
        }
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
        if (_livesUI != null)
        {
            _livesUI.gameObject.SetActive(false);
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
        _studentManager.OnPlayerDetected += OnPlayerDetected;
    }

    private void OnDisable()
    {
        _timeHelper.OnTimesUp -= OnTimesUp;
        _answerManager.OnAllPlayersFinishedAllAnswers -= OnAllPlayersFinishedAllAnswers;
        _studentManager.OnPlayerDetected -= OnPlayerDetected;
    }

    // Triggere externallyd when a player joins the game
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        PlayerController playerController = playerInput.GetComponent<PlayerController>();
        ChairController chairController = _answerManager.GetPlayerDesk(playerInput.playerIndex).transform.parent.GetComponentInChildren<ChairController>();
        playerController.SetInitialChairController(chairController);
        _players.Add(playerController);

        if (_gameCancellationSource != null) return; // Game already started

        if (_players.Count >= _answerManager.RequiredPlayersCount || !_globalDefinition.StartGameWhenAllPlayersJoined)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        if (_livesUI != null)
        {
            _livesUI.gameObject.SetActive(true);
        }
        SetLives(_globalDefinition.PlayerLives);
        _gameCancellationSource = new CancellationTokenSource();
        StartTimer();
        _studentManager.StartStimulation(_gameCancellationSource.Token);
    }

    private void StopGame()
    {
        _gameCancellationSource.Cancel();
    }

    private void RestartGame()
    {
        _answerManager.CleanActivePeeks();
        _answerManager.ResetProgress();
        if (_defeatFeedback != null)
        {
            _defeatFeedback.SetActive(false);
        }
        if (_victoryFeedback != null)
        {
            _victoryFeedback.SetActive(false);
        }
        if (_timesUpFeedback != null)
        {
            _timesUpFeedback.SetActive(false);
        }
        foreach (var player in _players)
        {
            player.TeleportToInitialChair();
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

    private void OnPlayerDetected(PlayerController playerController)
    {
        SetLives(_playerLives - 1);
        if (_playerLives > 0)
        {
            playerController.TeleportToInitialChair();
            return;
        }

        StopGame();
        if (_defeatFeedback != null)
        {
            _defeatFeedback.SetActive(true);
        }
    }

    private void SetLives(int value)
    {
        _playerLives = value;
        _livesUI.SetLives(value);
    }
}
