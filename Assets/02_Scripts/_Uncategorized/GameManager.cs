using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private AnswersManager _answerManager;
    [SerializeField] private StudentManager _studentManager;
    [SerializeField] private TeacherManager _teacherManager;

    [SerializeField] private GameObject _defeatFeedback;
    [SerializeField] private GameObject _victoryFeedback;
    [SerializeField] private TimeUI _timeUI;
    [SerializeField] private LivesUI _livesUI;
    [SerializeField] private HelpUI _helpUI;
    [SerializeField] private GameObject _timesUpFeedback;
    [SerializeField] private ButtonListener[] _restartButtons;

    [Header("Configuratinos")]
    [SerializeField] private GlobalDefinition _globalDefinition;
    [SerializeField] private float _maxTimeInSeconds = 30;

    private TimeHelper _timeHelper;
    private CancellationTokenSource _gameCancellationSource;
    private List<PlayerController> _players = new();
    private int _playerLives;

    public static GameManager Instance { get; private set; }
    public bool GameplayActive { get; private set; }

    [SerializeField] private MultiplayerEventSystem _eventSystem;

    private void Awake()
    {
        Instance = this;
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
        if (_helpUI != null)
        {
            _helpUI.Hide();
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
        _studentManager.OnItemDetected += OnItemDetected;
        _teacherManager.OnPlayerDetected += OnPlayerDetected;
        _teacherManager.OnItemDetected += OnItemDetected;
    }

    private void OnDisable()
    {
        _timeHelper.OnTimesUp -= OnTimesUp;
        _answerManager.OnAllPlayersFinishedAllAnswers -= OnAllPlayersFinishedAllAnswers;
        _studentManager.OnPlayerDetected -= OnPlayerDetected;
        _studentManager.OnItemDetected -= OnItemDetected;
        _teacherManager.OnPlayerDetected -= OnPlayerDetected;
        _teacherManager.OnItemDetected -= OnItemDetected;
    }

    // Triggere externallyd when a player joins the game
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        var inputHandler = playerInput.GetComponent<PlayerInputHandler>();
        if (inputHandler == null)
        {
            return;
        }

        PlayerController playerController = playerInput.GetComponent<PlayerController>();
        ChairController chairController = _answerManager.GetPlayerDesk(playerInput.playerIndex).transform.parent.GetComponentInChildren<ChairController>();
        playerController.SetInitialChairController(chairController);
        playerController.OnShowHelp += OnShowHelp;
        playerController.OnHideHelp += OnHideHelp;
        _players.Add(playerController);

        if (GameplayActive)
            return; // Game already started

        if (_players.Count >= _answerManager.RequiredPlayersCount || !_globalDefinition.StartGameWhenAllPlayersJoined)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        GameplayActive = true;
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
        GameplayActive = false;
        _gameCancellationSource?.Cancel();
        _gameCancellationSource = null;
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
        EnableGameplayInput();
    }

    private void StartTimer()
    {
        _timeHelper.Setup(_maxTimeInSeconds);
        _timeHelper.StartTimer(_gameCancellationSource.Token).Forget();
    }

    private void OnAllPlayersFinishedAllAnswers()
    {
        ShowEndMenu(_victoryFeedback);
    }

    private void OnTimesUp()
    {
        ShowEndMenu(_timesUpFeedback);
    }

    private void OnPlayerDetected(PlayerController playerController)
    {
        SetLives(_playerLives - 1);
        if (_playerLives > 0)
        {
            playerController.TeleportToInitialChair();
            return;
        }

        ShowEndMenu(_defeatFeedback);
    }

    private void OnItemDetected(IItemController itemController)
    {
        if (TryGetPlayerController(itemController.LastOwnerID, out PlayerController playerController))
        {
            OnPlayerDetected(playerController);
        }
    }

    private void SetLives(int value)
    {
        _playerLives = value;
        _livesUI.SetLives(value);
    }

    private void OnShowHelp(EDevice deviceType)
    {
        if (_helpUI != null)
        {
            _helpUI.Show(deviceType);
        }
    }

    private void OnHideHelp()
    {
        if (_helpUI != null)
        {
            _helpUI.Hide();
        }
    }

    private bool TryGetPlayerController(string id, out PlayerController playerController)
    {
        foreach (var player in _players)
        {
            if (player.ID == id)
            {
                playerController = player;
                return true;
            }
        }
        playerController = null;
        return false;
    }

    private void FocusFirstRestartButton()
    {
        if (_eventSystem == null)
        {
            Debug.LogWarning("No MultiplayerEventSystem in scene.");
            return;
        }

        _eventSystem.SetSelectedGameObject(null);

        foreach (var button in _restartButtons)
        {
            if (button != null && button.gameObject.activeInHierarchy)
            {
                _eventSystem.SetSelectedGameObject(button.gameObject);
                return;
            }
        }
    }

    private void DisableGameplayInput()
    {

        foreach (var playerInput in PlayerInput.all)
        {
            var handler = playerInput.GetComponent<PlayerInputHandler>();

            if (handler == null)
                continue;

            playerInput.DeactivateInput();
            playerInput.GetComponent<PlayerController>()?.ResetInputState();

        }
    }

    private void EnableGameplayInput()
    {
        foreach (var playerInput in PlayerInput.all)
        {
            if (playerInput.GetComponent<PlayerInputHandler>() == null)
                continue;

            playerInput.ActivateInput();
        }
    }

    private void ShowEndMenu(GameObject menu)
    {
        StopGame();
        DisableGameplayInput();

        if (menu != null)
        {
            menu.SetActive(true);
            FocusFirstRestartButton();
        }
    }
}
