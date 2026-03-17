using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using _02_Scripts.Tools;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private AnswersManager _answerManager;
    [SerializeField] private StudentManager _studentManager;
    [SerializeField] private TeacherManager _teacherManager;

    [SerializeField] private GameObject _defeatFeedback;
    [SerializeField] private VictoryUI _victoryUI;
    [SerializeField] private TimeUI _timeUI;
    [SerializeField] private RoundTimeUI _roundTimeUI;
    [SerializeField] private LivesUI _livesUI;
    [SerializeField] private HelpUI _helpUI;
    [SerializeField] private GameObject _timesUpFeedback;
    [SerializeField] private ButtonListener[] _restartButtons;
    [SerializeField] private PlayerAppearanceSO[] _playerAppearances;

    [Header("Configurations")]
    [SerializeField] private GlobalDefinition _globalDefinition;
    [SerializeField] private float _maxTimeInSeconds = 30;
    // [SerializeField] private float _maxRoundTimeInSeconds = 30;

    private TimeHelper _timeHelper;
    private RoundTimeHelper _roundTimeHelper;
    private CancellationTokenSource _gameCancellationSource;
    private CancellationTokenSource _roundCancellationSource;
    private List<PlayerController> _players = new();
    private int _playerLives;
    private Dictionary<PlayerController, FlashEffect> _playerFlashEffects = new();

    public static GameManager Instance { get; private set; }
    public bool GameplayActive { get; private set; }

    [SerializeField] private MultiplayerEventSystem _eventSystem;
    [SerializeField] private PlayerInputManager _playerInputManager;
    [SerializeField] private GameAudioHelper.Data _audioData;

    [Header("TEST ONLY")]
    [SerializeField] private TestDefinition _testDefinition;

    private GameAudioHelper _audioHelper;

    private void Awake()
    {
        InitializeSingleton();
        InitializeUIState();
        InitializeRestartButtons();
        InitializeHelpers();
        InjectTestDefinitionsIfNeeded();
    }

    private void OnEnable()
    {
        _timeHelper.OnTimesUp += OnTimesUp;
        _roundTimeHelper.OnRoundTimesUp += OnRoundTimesUp;
        _roundTimeHelper.OnPhaseChanged += HandlePhaseChanged;
        _roundTimeHelper.OnCountdownBeep += HandleCountdownBeep;
        _roundTimeHelper.OnLoopRestarted += HandleLoopRestarted;
        _answerManager.OnAllPlayersFinishedAllAnswers += OnAllPlayersFinishedAllAnswers;
        _studentManager.OnPlayerDetected += OnPlayerDetected;
        _studentManager.OnItemDetected += OnItemDetected;
        if (_teacherManager != null)
        {
            _teacherManager.OnPlayerDetected += OnPlayerDetected;
            _teacherManager.OnItemDetected += OnItemDetected;
        }
    }

    private void OnDisable()
    {
        _timeHelper.OnTimesUp -= OnTimesUp;
        _roundTimeHelper.OnRoundTimesUp -= OnRoundTimesUp;
        _roundTimeHelper.OnPhaseChanged -= HandlePhaseChanged;
        _roundTimeHelper.OnCountdownBeep -= HandleCountdownBeep;
        _roundTimeHelper.OnLoopRestarted -= HandleLoopRestarted;
        _answerManager.OnAllPlayersFinishedAllAnswers -= OnAllPlayersFinishedAllAnswers;
        _studentManager.OnPlayerDetected -= OnPlayerDetected;
        _studentManager.OnItemDetected -= OnItemDetected;
        if (_teacherManager != null)
        {
            _teacherManager.OnPlayerDetected -= OnPlayerDetected;
            _teacherManager.OnItemDetected -= OnItemDetected;
        }
    }

    // Triggere externallyd when a player joins the game
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        if (GameplayActive)
        {
            playerInput.DeactivateInput();
            return;
        }

        var inputHandler = playerInput.GetComponent<PlayerInputHandler>();
        if (inputHandler == null)
        {
            return;
        }

        inputHandler.Initialize();
        
        PlayerController playerController = playerInput.GetComponent<PlayerController>();
        playerController.Inject(_answerManager);
        
        var colorComponent = playerInput.GetComponent<ColorComponent>();
        var appearance = _playerAppearances[playerInput.playerIndex];
        colorComponent.SetColor(appearance.ClothesColor, "Clothes");
        colorComponent.SetColor(appearance.HairColor, "Hair");
        
        ChairController chairController = _answerManager.GetPlayerDesk(playerInput.playerIndex).transform.parent.GetComponentInChildren<ChairController>();
        playerController.SetInitialChairController(chairController);
        playerController.OnShowHelp += OnShowHelp;
        playerController.OnHideHelp += OnHideHelp;
        _players.Add(playerController);

        FlashEffect flashEffect = playerController.GetComponent<FlashEffect>();
        if (flashEffect != null)
            _playerFlashEffects[playerController] = flashEffect;


        if (_players.Count >= _answerManager.RequiredPlayersCount ||
            !_globalDefinition.StartGameWhenAllPlayersJoined)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        GameplayActive = true;

        DisablePlayerJoining();

        if (_livesUI != null)
        {
            _livesUI.gameObject.SetActive(true);
        }

        SetLives(_globalDefinition.PlayerLives);

        _gameCancellationSource = new CancellationTokenSource();
        StartTimer();
        StartRoundTimer();
        _studentManager.StartStimulation(_gameCancellationSource.Token);

        EnableGameplayInput();

    }

    private void StopGame()
    {
        GameplayActive = false;
        _gameCancellationSource?.Cancel();
        _gameCancellationSource = null;
    }

    private void RestartGame()
    {
        StopGame();
        StopRoundTimer();

        _answerManager.CleanActivePeeks();
        _answerManager.ResetProgress();

        if (_teacherManager != null)
        {
            _teacherManager.ResetTeachers();
        }
        if (_defeatFeedback != null)
        {
            _defeatFeedback.SetActive(false);
        }
        if (_victoryUI != null)
        {
            _victoryUI.Hide();
        }
        if (_timesUpFeedback != null)
        {
            _timesUpFeedback.SetActive(false);
        }

        foreach (var player in _players)
        {
            player.ForceClearInteractionState();
            player.TeleportToInitialChair();
        }

        StartGame();
    }

    private void StartTimer()
    {
        _timeHelper.Setup(_maxTimeInSeconds);
        _timeHelper.StartTimer(_gameCancellationSource.Token).Forget();
    }

    private void StartRoundTimer()
    {
        _roundCancellationSource?.Cancel();
        _roundCancellationSource = new CancellationTokenSource();
        _roundTimeHelper.IsLooping = true; // 👈 ensure this is set
        _roundTimeHelper.Setup(_globalDefinition);
        _roundTimeHelper.StartTimer(_roundCancellationSource.Token).Forget();
    }

    private void StopRoundTimer()
    {
        _roundCancellationSource?.Cancel();
        _roundCancellationSource = null;
    }
    private void HandlePhaseChanged(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 0: // PreAnswering → Answering
                _audioHelper.OnPhaseChangeAnswer(); 
                break;
            case 1: // Answering → PostAnswering
                _audioHelper.OnPhaseChangeCheat();  
                break;
        }
    }

    private void HandleLoopRestarted()
    {
        _audioHelper.OnPhaseChangeThink();
    }
    private void HandleCountdownBeep(int phaseIndex, int secondsLeft)
    {
        // Play a different final beep on 1, ticks on 2 and 3
        if (secondsLeft == 1)
            _audioHelper.BeepFinal();
        else
            _audioHelper.BeepNotFinal();

        // Optionally do something different per phase
        // e.g. urgent beep only during answering phase
        // if (phaseIndex == 1) AudioManager.Instance.Play("beep_urgent");
    }

    private void OnAllPlayersFinishedAllAnswers(float minCorrectness)
    {
        if (minCorrectness < _globalDefinition.MinCorrectnessToEarlyVictoryFlow) return;

        _victoryUI.UpdateAnswerSheets();
        ShowEndMenu(_victoryUI.gameObject);
    }

    private void OnTimesUp()
    {
        ShowEndMenu(_timesUpFeedback);
        StopRoundTimer();
    }
    private async void OnRoundTimesUp()
    {
        //await UniTask.Yield();
        //StartRoundTimer();
    }

    private void OnPlayerDetected(PlayerController playerController)
    {
        if (playerController.IsSitting) return; // Cannot lose life while 
        LoseLife(playerController);
    }

    private void OnItemDetected(IItemController itemController)
    {
        if (itemController is not PaperBallController paperBall)
            return;

        //Sitting players are safe ONLY if the ball was NOT thrown
        foreach (var player in _players)
        {
            if (player.IsSitting && !paperBall.HasBeenThrown())
                return;
        }

        //Confiscate thrown paperballs
        paperBall.OnDetectedByTeacher();

        //Punish owner if known
        if (!TryGetPlayerController(itemController.LastOwnerID, out PlayerController owner))
            return;

        LoseLife(owner);
    }
    private void LoseLife(PlayerController playerController)
    {
        SetLives(_playerLives - 1);
        if (_playerLives > 0)
        {
            if (_playerFlashEffects.TryGetValue(playerController, out var flash))
                flash.Flash();

            playerController.OnCaught(onAfterTeleport: null);
            return;
        }

        ShowEndMenu(_defeatFeedback);
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
        GameplayActive = false;

        DisableGameplayInput(); // STOP PLAYERS
        DisablePlayerJoining(); // STOP NEW PLAYERS

        _gameCancellationSource?.Cancel();
        _gameCancellationSource = null;
        _audioHelper.OnGameEnd();

        if (menu != null)
        {
            menu.SetActive(true);
            FocusFirstRestartButton();
        }
    }

    private void EnablePlayerJoining()
    {
        if (_playerInputManager != null)
            _playerInputManager.enabled = true;
    }

    private void DisablePlayerJoining()
    {
        if (_playerInputManager != null)
            _playerInputManager.enabled = false;
    }

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple GameManager instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void InitializeUIState()
    {
        if (_defeatFeedback != null)
        {
            _defeatFeedback.SetActive(false);
        }
        if (_victoryUI != null)
        {
            _victoryUI.Hide();
        }
        if (_timesUpFeedback != null)
        {
            _timesUpFeedback.SetActive(false);
        }
        if (_timeUI != null)
        {
            _timeUI.gameObject.SetActive(false);
        }
        if (_roundTimeUI != null)
        {
            _roundTimeUI.gameObject.SetActive(false);
        }
        if (_livesUI != null)
        {
            _livesUI.gameObject.SetActive(false);
        }
        if (_helpUI != null)
        {
            _helpUI.Hide();
        }
    }

    private void InitializeRestartButtons()
    {
        foreach (var button in _restartButtons)
        {
            if (button == null)
            {
                continue;
            }

            button.OnClickEvent += RestartGame;
        }
    }

    private void InitializeHelpers()
    {
        _timeHelper = new TimeHelper(_timeUI);
        _roundTimeHelper = new RoundTimeHelper(_roundTimeUI);
        _audioHelper = new GameAudioHelper(_audioData);
    }

    private void InjectTestDefinitionsIfNeeded()
    {
#if UNITY_EDITOR
        bool useTest = UnityEditor.EditorPrefs.GetBool(GameToolsMenu.UseTestDefinitionKey, false);
#else
        bool useTest = false;
#endif
        if (!useTest || _testDefinition == null)
        {
            return;
        }

        if (_answerManager != null)
        {
            _answerManager.InjectTestDefinition(_testDefinition);
        }

        if (_studentManager != null)
        {
            _studentManager.InjectTestDefinition(_testDefinition);
        }
    }

}
