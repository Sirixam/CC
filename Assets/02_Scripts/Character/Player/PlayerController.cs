using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour, IInteractionActor, IThrowActor
{
    private struct AimInput
    {
        public Vector2 Input;
        public bool IsMouse;
    }

    [SerializeField] private PlayerView _view;
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private PlayerPhysics _physics;
    [SerializeField] private FieldOfViewController _fieldOfViewController;

    [Header("Triggers")]
    [SerializeField] private TriggerListener _interactionTriggerListener;
    [SerializeField] private TriggerListener _fovTriggerListener;
    [SerializeField] private TriggerListener _selfTriggerListener; // passive — others detect the player through this

    [Header("Data")]
    [SerializeField] private InteractionHelper.Data _interactionData;
    [SerializeField] private ThrowHelper.Data _throwData;
    [SerializeField] private LobThrowHelper.Data _lobThrowData;
    [SerializeField] private DynamicLobThrowHelper.Data _dynamicLobThrowData;
    [SerializeField] private StunHelper.Data _stunData;
    [SerializeField] private PlayerCheatHelper.Data _cheatData;
    [SerializeField] private DashHelper.Data _dashData;
    [SerializeField] private LookHelper.Data _lookData;
    [SerializeField] private PlayerAudioHelper.Data _audioData;



    [SerializeField] private GlobalDefinition _globalDefinition;
    [Header("Peek")]
    [SerializeField] private float _peekMoveSpeedMultiplier = 0.5f;
    [Header("Push")]
    [SerializeField] private float _pushForce = 5f;
    [SerializeField] private float _caughtCheatingForce = 15f;
    [Header("TO BE REMOVED")]
    [SerializeField] private bool _dropByHoldingInteract; // Once we decide on the final input scheme, this can be removed
    [SerializeField] private bool _toggleToPeek;
    [SerializeField] private bool _stopPeekOnDash;
    [SerializeField] private bool _stopPeekOnTeleport;
    [Tooltip("If TRUE penalty will apply while peek mode is active, if FALSE it will apply only while peeking a student.")]
    [SerializeField] private bool _applyMovePenaltyOnPeekMode;

    private LobThrowHelper _lobThrowHelper;
    private DynamicLobThrowHelper _dynamicLobThrowHelper;

    private bool _isCaught;
    public bool IsCaught => _isCaught;
    private Coroutine _walkBackTimeout;


    //exposing variables to GM
    public PlayerView View => _view;
    public PlayerInputHandler InputHandler => _inputHandler;


    // Runtime
    private AnswerController _answerController;
    private ChairController _initialChairController;
    private List<InteractionController> _interactionsInsideFoV = new();

    private AimInput _lastAimInput;
    private bool _skipNextDirectionAction;
    private bool IsPeeking => _inputHandler.ScopeType == EInputScope.PlayerPeeking;
    private bool IsAnswering => _answerController != null && _answerController.IsAnswering;
    public bool IsSitting => _chairHelper.IsSitting;
    public TriggerListener SelfTriggerListener => _selfTriggerListener;

    // Helpers
    private InteractionHelper _interactionHelper;
    private ThrowHelper _throwHelper;
    private StunHelper _stunHelper;
    private PlayerCheatHelper _cheatHelper;
    private LookHelper _lookHelper;
    private DashHelper _dashHelper;
    private ChairHelper _chairHelper;
    private CraftHelper _craftHelper;
    private PlayerAudioHelper _audioHelper;

    // IActor
    public string ID => IActor.GetPlayerID(_inputHandler.PlayerInput.playerIndex);
    // IInteractionActor
    Vector3 IInteractionActor.Position => transform.position;
    Vector3 IInteractionActor.Forward => transform.forward;
    // IThrowActor
    Vector3 IThrowActor.LookDirection => _view.transform.forward;
    Collider[] IThrowActor.Colliders => _physics.Colliders;

    public event Action<EDevice> OnShowHelp;
    public event Action OnHideHelp;
    private Action _onWalkBackSeated;
    public Transform InitialChairTransform => _initialChairController != null
        ? _initialChairController.SittingPoint
        : null;

    private bool _isWalkingBack;

    private void Awake()
    {
        _physics.Initialize();
        _interactionHelper = new InteractionHelper(_interactionData, this, isEnabled: true);
        _throwHelper = new ThrowHelper(_throwData, this, _interactionHelper, _globalDefinition.FlyingLayer);
        _chairHelper = new ChairHelper(_inputHandler, _view, _physics);
        _stunHelper = new StunHelper(_stunData, _view);
        _cheatHelper = new PlayerCheatHelper(_cheatData, _view);
        _lookHelper = new LookHelper(_lookData);
        _audioHelper = new PlayerAudioHelper(_audioData);
        _dashHelper = new DashHelper(_dashData, _view, _physics, _lookHelper, _audioHelper);
        _craftHelper = new CraftHelper(this, _view, _interactionHelper, GameContext.ItemsManager, _globalDefinition);
        _lobThrowHelper = new LobThrowHelper(_lobThrowData, this, _interactionHelper, _globalDefinition.FlyingLayer);
        _dynamicLobThrowHelper = new DynamicLobThrowHelper(_dynamicLobThrowData, this, _interactionHelper, _globalDefinition.FlyingLayer);



        // Initialize
        _view.InitializeThrowPreview(_chairHelper, _throwData, _globalDefinition.FlyingLayer);
        _view.InitializeLobThrowPreview(_chairHelper, _lobThrowHelper, _globalDefinition.FlyingLayer);
        _view.InitializeDynamicLobThrowPreview(_chairHelper, _dynamicLobThrowHelper, _globalDefinition.FlyingLayer);


        _lookHelper.Initialize(transform.forward);
        _fieldOfViewController.HideInstant();
        TeleportToInitialChair();

    }

    public void Inject(IAnswerIconProvider answerIconProvider)
    {
        _view.Inject(answerIconProvider);
    }

    private void OnEnable()
    {
        _inputHandler.ActionEvent += OnActionRequested;
        _inputHandler.DirectionalActionEvent += OnDirectionalActionRequested;
        _inputHandler.PreHoldActionEvent += OnPreHoldActionDetected;
        _inputHandler.HoldActionEvent += OnHoldActionRequested;

        _interactionTriggerListener.OnEnter += OnInteractionTriggerEnter;
        _interactionTriggerListener.OnExit += OnInteractionTriggerExit;
        _fovTriggerListener.OnEnter += OnFovTriggerEnter;
        _fovTriggerListener.OnExit += OnFovTriggerExit;

        _chairHelper.OnSittingComplete += TryShowAnswerSheetOnSit;
        _craftHelper.OnFinishedCrafting += TryShowAnswerSheetOnSit;
        _craftHelper.OnFinishedCrafting += _view.StopCrafting;

    }

    private void OnDisable()
    {
        _inputHandler.ActionEvent -= OnActionRequested;
        _inputHandler.DirectionalActionEvent -= OnDirectionalActionRequested;
        _inputHandler.PreHoldActionEvent -= OnPreHoldActionDetected;
        _inputHandler.HoldActionEvent -= OnHoldActionRequested;

        _interactionTriggerListener.OnEnter -= OnInteractionTriggerEnter;
        _interactionTriggerListener.OnExit -= OnInteractionTriggerExit;
        _fovTriggerListener.OnEnter -= OnFovTriggerEnter;
        _fovTriggerListener.OnExit -= OnFovTriggerExit;

        _chairHelper.OnSittingComplete -= TryShowAnswerSheetOnSit;
        _craftHelper.OnFinishedCrafting -= TryShowAnswerSheetOnSit;
        _craftHelper.OnFinishedCrafting -= _view.StopCrafting;
    }

    private void OnActionRequested(EAction actionType)
    {
        if (actionType == EAction.Dash)
        {
            if (!_dashHelper.CanDash()) return;
            _dashHelper.StartDash();
            if (IsPeeking && _stopPeekOnDash)
            {
                RestoreInputScope();
            }
        }
        else if (actionType == EAction.Interact)
        {
            if (_cheatHelper.IsCheating)
            {
                StopCheating();
            }
            else if (_chairHelper.IsSitting)
            {
                HideAnswerSheet();
                TryShowAnswerSheetOnSit();
            }
            else
            {
                TryStartInteraction();
            }
        }
        else if (actionType == EAction.Action)
        {
            if (!_dropByHoldingInteract)
            {
                RestoreInputScope();
                TryDropItem();
            }
        }
        else if (actionType == EAction.Cancel)
        {
            if (_inputHandler.ScopeType == EInputScope.PlayerAiming)
            {
                _dynamicLobThrowHelper.StopCharging();
                RestoreInputScope();
                _inputHandler.CancelActionHold();
                _view.HideThrowPreview();
                _view.HideLobThrowPreview();
                _view.HideDynamicLobThrowPreview();
                TryShowAnswerSheetOnSit();
            }
            else if (_inputHandler.ScopeType == EInputScope.PlayerSitting)
            {
                RequestStanding();
            }
        }
        else if (actionType == EAction.Peek)
        {
            if (!_toggleToPeek && IsPeeking)
            {
                RestoreInputScope();
                _inputHandler.CancelPeekHold();
            }
        }
        else if (actionType == EAction.Utility)
        {
            if (_craftHelper.TryStopCraftingItem())
                _view.StopCrafting();
            TryShowAnswerSheetOnSit();
        }
        else if (actionType == EAction.Help)
        {
            OnHideHelp?.Invoke();
        }
    }

    private void TryStartInteraction()
    {
        InteractionController interaction = _interactionHelper.BestInteraction;
        if (interaction == null) return;

        if (interaction.Type == EInteraction.PickUp)
        {
            // GUARDRAIL: player can only carry one item
            if (IsHoldingItem())
                return;

            _view.OnPickUp(interaction.transform);
            _interactionHelper.StartInteraction(interaction);
            _audioHelper.OnPickUp();
            if (interaction.TryGetComponent(out IPickUpInteractionOwner interactionOwner))
            {
                interactionOwner.OnPickedUp(ID);
            }
        }
        else if (interaction.Type == EInteraction.Static)
        {
            if (TryRequestStaticInteraction(interaction))
            {
                _interactionHelper.StartInteraction(interaction);
            }
        }
        else
        {
            Debug.LogError("Interaction type is not being handled: " + interaction.Type);
        }
    }

    private void TryStartInteractionOnHold()
    {
        InteractionController interaction = _interactionHelper.BestInteraction;
        if (interaction == null) return;

        if (interaction.Type == EInteraction.Static)
        {
            if (RequestStaticInteractionOnHold(interaction))
            {
                _interactionHelper.StartInteraction(interaction);
            }
        }
        else if (interaction.Type != EInteraction.PickUp)
        {
            Debug.LogError("Interaction type is not being handled: " + interaction.Type);
        }
    }

    private bool TryRequestStaticInteraction(InteractionController interaction)
    {
        if (interaction.TryGetComponent(out ChairController chairController))
        {
            if (chairController.CanPlayerSit && CanApproachChair(chairController))
            {
                RequestSitting(chairController);
                return true;
            }
            return false;  // ← blocked approach or can't sit
        }

        if (interaction.TryGetComponent<AnswerController>(out _))
        {
            return false;
        }

        Debug.LogError("Static interaction is not being handled: " + interaction.name);
        return false;
    }

    private bool RequestStaticInteractionOnHold(InteractionController interaction)
    {
        if (interaction.TryGetComponent(out AnswerController answerController))
        {
            if (_cheatHelper.CanStartCheating(answerController))
            {
                _lookHelper.SetLookAt(answerController.LookAtPoint);
                _cheatHelper.StartCheating(answerController);
            }
            return true;
        }

        if (interaction.TryGetComponent<ChairController>(out _))
        {
            // Empty for now
            return false;
        }

        Debug.LogError("Static interaction is not being handled: " + interaction.name);
        return false;
    }

    private void StartPeeking()
    {
        _lookHelper.ClearLookAt();
        _inputHandler.SetScope(EInputScope.PlayerPeeking);
        _fieldOfViewController.Show();
        _audioHelper.OnStartPeeking();
    }

    private void StopPeeking()
    {
        _lookHelper.ClearLookAt();
        _cheatHelper.StopPeeking();
        _audioHelper.OnStopPeeking();
        StopStaticInteraction();
    }

    private bool CanStartAnswering(string answerID, float correctness, string contributorActorID, string sourceID)
    {
        return _answerController.CanStartAnswering(answerID, correctness, contributorActorID, sourceID, out _);
    }

    private void StartAnswering(string answerID, float correctness, string contributorActorID, string sourceID)
    {
        if (_answerController.TryStartAnswering(answerID, correctness, contributorActorID, sourceID))
        {
            _answerController.TestPageView.Lower();
            _audioHelper.OnStartAnswering();
            _view.StartWriting();
        }
    }

    private void ShowAnswerSheet()
    {
        if (_answerController == null) return;
        _answerController.ShowOrLiftAnswerSheet();
        _view.OnLiftAnswerSheet();
    }

    private void HideAnswerSheet()
    {
        if (_answerController == null)
            return;

        _answerController.HideOrLowerAnswerSheet();
        _view.OnLowerAnswerSheet();
        StopAnswering();
    }

    private void StopAnswering()
    {
        _answerController.StopAnswering();
        _audioHelper.TryStopAnswering();
        _view.StopWriting();
    }

    private void StopCheating()
    {
        _lookHelper.ClearLookAt();
        _cheatHelper.StopCheating();
        StopStaticInteraction();
    }

    public void SetInitialChairController(ChairController value)
    {
        _initialChairController = value;
    }

    public void TeleportToInitialChair()
    {
        if (_initialChairController == null) return;

        if (_initialChairController.IsBlocked)
        {
            _initialChairController.Unblock();
            Debug.LogError("Initial chair controller was blocked");
            return;
        }

        TeleportToChair(_initialChairController);
    }

    private void TeleportToChair(ChairController chairController)
    {
        if (_stopPeekOnTeleport)
        {
            RestoreInputScope(instant: true);
        }

        _lookHelper.SetLookAt(chairController.LookAtPoint);
        _chairHelper.TeleportToSitting(chairController);
        _interactionHelper.DisableInteraction();
        _answerController = chairController.AnswerController;

        if (_globalDefinition.ShowAnswerSheetOnSit)
            ShowAnswerSheet();
    }

    public void TeleportToDoor(Transform doorPoint)
    {
        if (_stopPeekOnTeleport)
        {
            RestoreInputScope(instant: true);
        }

        _lookHelper.ClearLookAt();
        _chairHelper.TeleportToStanding(doorPoint);
        _interactionHelper.EnableInteraction();

        if (_answerController != null)
        {
            HideAnswerSheet();
            _answerController = null;
        }
    }

    private void RequestSitting(ChairController chairController)
    {
        _lookHelper.SetLookAt(chairController.LookAtPoint);
        _chairHelper.StartSitting(chairController);
        _interactionHelper.DisableInteraction();
        _answerController = chairController.AnswerController;
        _audioHelper.OnStartSitting();

    }

    private void RequestStanding()
    {
        Vector2 inputDir = _inputHandler.LastMoveInput;
        _lookHelper.ClearLookAt();
        _chairHelper.StartStanding(inputDir);
        _interactionHelper.EnableInteraction();
        if (_answerController != null)
        {
            HideAnswerSheet();
            _answerController = null;
        }
        StopStaticInteraction();
    }

    private void StopStaticInteraction()
    {
        if (_interactionHelper.TryGetStaticInteraction(out InteractionController stoppedInteraction))
        {
            _interactionHelper.TryStopInteraction(stoppedInteraction);
        }
    }

    private void RestoreInputScope(bool instant = false)
    {
        // Handle specific cases
        if (_inputHandler.ScopeType == EInputScope.PlayerPeeking)
        {
            StopPeeking();
            if (instant)
            {
                _fieldOfViewController.HideInstant();
            }
            else
            {
                _fieldOfViewController.Hide();
            }
            _skipNextDirectionAction = true; // [AKP] This is a HACK to prevent the directional after using or cancelling the peek. Otherwise the character is rotated in the wrong direction.
        }

        // Restore
        if (_chairHelper.IsSitting)
        {
            _lookHelper.SetLookAt(_chairHelper.LookAtPoint);
            _inputHandler.SetScope(EInputScope.PlayerSitting);
        }
        else
        {
            _inputHandler.SetScope(EInputScope.PlayerStanding);
        }
    }

    private void OnDirectionalActionRequested(EDirectionalAction actionType, Vector2 input, bool isMouse)
    {
        // Debug.Log($"Directional input: {actionType} {input}");
        if (_skipNextDirectionAction)
        {
            _skipNextDirectionAction = false;
            return;
        }
        if (actionType == EDirectionalAction.Move)
        {
            _physics.SetMoveDirection(new Vector3(input.x, 0, input.y));
            if (!IsPeeking || !_lastAimInput.IsMouse)
            {
                _physics.SetInputDirection(new Vector3(input.x, 0, input.y));
                _lookHelper.SetLookInput(input);
            }
        }
        else if (actionType == EDirectionalAction.Aim)
        {
            ProcessAimInput(input, isMouse);
            _lastAimInput = new AimInput { Input = input, IsMouse = isMouse };
        }
    }

    private void ProcessAimInput(Vector2 input, bool isMouse)
    {
        if (isMouse)
        {
            Ray ray = Camera.main.ScreenPointToRay(input);
            if (!Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.LogError("Failed to get position from mouse input");
                return;
            }
            input = new Vector3(hit.point.x - transform.position.x, hit.point.z - transform.position.z).normalized;
        }

        _physics.SetInputDirection(new Vector3(input.x, 0, input.y));
        _lookHelper.SetLookInput(input);
    }

    private void OnPreHoldActionDetected(EAction actionType)
    {
        if (actionType == EAction.Interact)
        {
            if (_chairHelper.IsSitting)
            {
                if (_interactionHelper.TryGetPickedUpInteraction(out PaperBallController paperBallController) && paperBallController.HasAnswer && CanStartAnswering(paperBallController.AnswerID, paperBallController.Correctness, paperBallController.ContributorActorID, paperBallController.ID))
                {
                    StartAnswering(paperBallController.AnswerID, paperBallController.Correctness, paperBallController.ContributorActorID, paperBallController.ID);
                }
                else if (_cheatHelper.TryGetRememberedAnswer(out string answerID, out float correctness, out string actorID) && CanStartAnswering(answerID, correctness, actorID, sourceID: null))
                {
                    StartAnswering(answerID, correctness, actorID, sourceID: null); // Source is null because it's answered from memory and we already pass the actorID.
                }
                else
                {
                    ShowAnswerSheet();
                }
            }
            else
            {
                TryStartInteractionOnHold();
            }
        }
        else if (actionType == EAction.Action)
        {
            if (_chairHelper.IsSitting && !_globalDefinition.CanThrowWhileSeated)
                return;

            if (_interactionHelper.TryGetPickedUpInteraction(out _))
            {
                _lookHelper.ClearLookAt();
                _inputHandler.SetScope(EInputScope.PlayerAiming);
            }
        }
        else if (actionType == EAction.Peek)
        {
            if (IsPeeking)
            {
                RestoreInputScope();
                _inputHandler.CancelPeekHold();
            }
            else
            {
                StartPeeking();
            }
        }
        else if (actionType == EAction.Utility)
        {
            bool hasMemory = _cheatHelper.TryGetRememberedAnswer(out _, out _, out _);

            if (!hasMemory)
            {
                if (_globalDefinition.ShowAnswerSheetOnSit)
                    HideAnswerSheet();

                string craftItem = _globalDefinition.CraftedPaperBallType switch
                {
                    GlobalDefinition.EPaperBallType.LobShot => "Paper Ball LobShot",
                    GlobalDefinition.EPaperBallType.DynamicLobShot => "Paper Ball DynamicLobShot",
                    _ => "Paper Ball"
                };

                if (_craftHelper.TryStartCraftingItem(craftItem))
                    _view.StartCrafting();
            }
        }
        else if (actionType == EAction.Help)
        {
            OnShowHelp?.Invoke(_inputHandler.LastKnownDeviceType);
        }
    }

    private void OnHoldActionRequested(EAction actionType, bool isHolding)
    {
        if (actionType == EAction.Interact)
        {
            if (_chairHelper.IsSitting)
            {
                if (!isHolding)
                {
                    if (!_globalDefinition.ShowAnswerSheetOnSit)
                    {
                        HideAnswerSheet();
                    }
                    else
                    {
                        _answerController.ShowOrLiftAnswerSheet();
                        StopAnswering();
                    }
                }
            }
            else if (_cheatHelper.IsCheating)
            {
                if (!isHolding)
                {
                    StopCheating();
                }
            }
            else if (_dropByHoldingInteract)
            {
                TryDropItem();
            }
        }
        else if (actionType == EAction.Action)
        {
            if (_chairHelper.IsSitting && !_globalDefinition.CanThrowWhileSeated)
                return;

            if (!isHolding)
            {
                TryShowAnswerSheetOnSit();
                RestoreInputScope();
                _view.HideLobThrowPreview();
                _view.HideDynamicLobThrowPreview();
                _view.HideThrowPreview();
                if (!_dynamicLobThrowHelper.TryTriggerThrow())
                    if (!_lobThrowHelper.TryTriggerThrow())
                        _throwHelper.TryTriggerThrow();
            }
            else if (_dynamicLobThrowHelper.CanShowPreview())
            {
                _dynamicLobThrowHelper.StartCharging();
                _view.ShowDynamicLobThrowPreview();
                HideAnswerSheet();
            }
            else if (_lobThrowHelper.CanShowPreview())
            {
                _view.ShowLobThrowPreview();
                HideAnswerSheet();
            }
            else if (_throwHelper.CanShowPreview())
            {
                _view.ShowThrowPreview();
                HideAnswerSheet();
            }
        }
        else if (actionType == EAction.Peek)
        {
            if (!isHolding && !_toggleToPeek)
            {
                RestoreInputScope();
            }
        }
        else if (actionType == EAction.Utility)
        {
            if (!isHolding)
            {
                TryShowAnswerSheetOnSit();
                if (_craftHelper.TryStopCraftingItem())
                    _view.StopCrafting();
            }
        }
        else if (actionType == EAction.Help)
        {
            if (!isHolding)
            {
                OnHideHelp?.Invoke();
            }
        }
    }

    private void TryDropItem()
    {
        if (_interactionHelper.TryGetPickedUpInteraction(out InteractionController stoppedInteraction))
        {
            _interactionHelper.TryStopInteraction(stoppedInteraction);
            _view.OnDrop(stoppedInteraction.transform);
            _view.HideThrowPreview();
            if (stoppedInteraction.TryGetComponent(out IPickUpInteractionOwner interactionOwner))
            {
                interactionOwner.OnDropped();
            }

            // Small random nudge so the ball doesn't bounce in place
            Vector3 nudge = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                0f,
                UnityEngine.Random.Range(-1f, 1f)
            );
            stoppedInteraction.Rigidbody.AddForce(nudge, ForceMode.VelocityChange);
        }
    }

    private void Update()
    {

        if (!GameManager.Instance.GameplayActive)
            return;

        _dashHelper.UpdateCooldown();

        if (_stunHelper.IsStunned || (_isCaught && !_isWalkingBack))
        {
            if (_stunHelper.IsStunned)
                _stunHelper.UpdateStun();
            return;
        }

        if (IsPeeking && _lastAimInput.IsMouse)
        {
            ProcessAimInput(_lastAimInput.Input, _lastAimInput.IsMouse); // [AKP] Force aim to stick on current mouse position while moving
        }

        if (_isWalkingBack && _physics.IsFollowingPath)
        {
            Vector3 dir = _physics.PathDirection;
            if (dir.sqrMagnitude > 0.01f)
            {
                _lookHelper.SetLookInput(new Vector2(dir.x, dir.z));
            }
        }

        _lookHelper.UpdateRotation(transform);
        _interactionHelper.UpdateBestInteraction();

        if (IsAnswering && _answerController.ActiveAnswerID != null)
        {
            // Scale deltaTime so the answer always finishes in _cheatData.AnswerDuration,
            // regardless of the answer's BaseAnswerDuration.
            float baseAnswerDuration = _answerController.GetAnsweringDuration();
            float deltaTime = Time.deltaTime * (baseAnswerDuration / _cheatData.AnswerDuration);

            _answerController.UpdateAnswering(deltaTime, out bool finishedAnswering);

            if (finishedAnswering)
            {
                _answerController.StartIdle();
                _audioHelper.OnFinishedCorrectAnswer();
                _view.StopWriting();
                if (_cheatHelper.TryGetRememberedAnswer(out string answerID, out float correctness, out string actorID))
                {
                    TryDropItem(); // drop whatever is held
                    _cheatHelper.StopRemembering();
                    PaperBallController answerInstance = _craftHelper.CraftAnswer(answerID, correctness, actorID);
                    _answerController.AddContributor(answerID, answerInstance.ID);
                }
            }
        }

        bool applyPeekMoveSpeedMultiplier = _applyMovePenaltyOnPeekMode ? IsPeeking : _cheatHelper.IsPeeking;
        _physics.SetMoveSpeedMultiplier(applyPeekMoveSpeedMultiplier ? _peekMoveSpeedMultiplier : 1f);

        if (IsPeeking)
        {
            UpdateFovPeeking();
        }
        if (_cheatHelper.IsPeeking)
        {
            _cheatHelper.UpdatePeeking(out bool finishedPeeking);
            if (finishedPeeking)
            {
                StopPeeking();
            }
        }
        if (_cheatHelper.IsCheating)
        {
            if (_cheatHelper.IsCheatBlocked)
            {
                if (_caughtCheatingForce > 0)
                {
                    Vector3 forceDirection = (_physics.Position - _cheatHelper.AnswerPosition).normalized;
                    _physics.StartExternalForce(forceDirection * _caughtCheatingForce);
                }
                StopCheating(); // Trigger after external force
                return;
            }

            _cheatHelper.UpdateCheating(out bool finishedCheating);
            if (finishedCheating)
            {
                StopCheating();
                _audioHelper.OnStartCheating();
            }
        }
        if (_cheatHelper.IsRemembering && !IsAnswering)
        {
            _cheatHelper.UpdateMemory(out _);
        }
        if (_dynamicLobThrowHelper.IsCharging)
        {
            _dynamicLobThrowHelper.UpdateCharging();
        }
        if (_craftHelper.IsCrafting)
        {
            _craftHelper.UpdateCrafting(Time.deltaTime);
        }
    }
    private void FixedUpdate()
    {
        if (!GameManager.Instance.GameplayActive)
            return;

        bool canMove = !_stunHelper.IsStunned && !_isCaught;
        _physics.OnFixedUpdate(Time.fixedDeltaTime, canMove: !_stunHelper.IsStunned && (!_isCaught || _isWalkingBack), out bool stoppedForce);
        if (stoppedForce)
        {
            _view.OnStopForce();
        }
    }

    private void OnCollisionStay(Collision collision)
        => _dashHelper.OnCollisionStay(collision, onStopDash: _stunHelper.StartStun);

    private void OnCollisionEnter(Collision collision)
    {
        // Walk-back push logic
        if (_isWalkingBack)
        {
            PlayerController otherPlayer = collision.gameObject.GetComponentInParent<PlayerController>();
            if (otherPlayer != null && otherPlayer != this && !otherPlayer.IsCaught)
            {
                Vector3 walkDir = _physics.PathDirection;
                Vector3 toOther = (otherPlayer.transform.position - transform.position).normalized;
                float cross = Vector3.Cross(walkDir, toOther).y;
                Vector3 pushDir = cross >= 0
                    ? new Vector3(walkDir.z, 0, -walkDir.x)
                    : new Vector3(-walkDir.z, 0, walkDir.x);
                if (Physics.Raycast(otherPlayer.transform.position, pushDir, 1f))
                    pushDir = -pushDir;
                otherPlayer.OnPushedAside(pushDir);
            }
            return;
        }

        // Teacher collision detection
        if (!_isCaught && !_chairHelper.IsSitting)
        {
            TeacherController teacher = collision.gameObject.GetComponentInParent<TeacherController>();
            if (teacher != null)
            {
                teacher.OnPlayerCollided(this);
                return;
            }
        }

    }

    private void OnInteractionTriggerEnter(Collider other)
    {
        _interactionHelper.TryAddInteraction(other, out _);
    }

    private void OnInteractionTriggerExit(Collider other)
    {
        _interactionHelper.TryRemoveInteraction(other, out _);
    }

    private void OnFovTriggerEnter(Collider other)
    {
        if (!_interactionHelper.TryGetInteraction(other, out InteractionController interaction)) return;
        _interactionsInsideFoV.Add(interaction);
    }

    private void OnFovTriggerExit(Collider other)
    {
        if (!_interactionHelper.TryGetInteraction(other, out InteractionController interaction)) return;
        _interactionsInsideFoV.Remove(interaction);
    }

    private void UpdateFovPeeking()
    {
        _interactionsInsideFoV.RemoveAll(x => x == null);

        InteractionController bestFov = _interactionHelper.ComputeBestInteractionForFOV(_interactionsInsideFoV);

        _interactionHelper.TryGetStaticInteraction(out InteractionController currentPeek);

        if (bestFov == currentPeek) return;

        if (_cheatHelper.IsPeeking)
        {
            _cheatHelper.StopPeeking();
        }
        if (currentPeek != null)
        {
            _interactionHelper.TryStopInteraction(currentPeek);
        }
        if (bestFov != null && bestFov.TryGetComponent(out AnswerController answerController) && _cheatHelper.CanStartPeeking(answerController))
        {
            _cheatHelper.StartPeeking(answerController);
            _interactionHelper.StartInteraction(bestFov);
        }
    }

    void IThrowActor.OnThrow(Transform thrownTransform)
        => _view.OnThrow(thrownTransform);

    public void ResetInputState()
    {
        _physics.SetInputDirection(Vector3.zero);
        _physics.SetMoveDirection(Vector3.zero);
        _lookHelper.SetLookInput(Vector2.zero);
    }
    public void ForceClearInteractionState()
    {
        _answerController = null;
        _interactionHelper?.DisableInteraction();
        _inputHandler.CancelActionHold();
        _inputHandler.CancelPeekHold();
    }
    private bool IsHoldingItem()
    {
        return _interactionHelper.TryGetPickedUpInteraction(out _);
    }

    public void OnCaught(Action onAfterTeleport, Transform doorPoint = null)
    {
        if (_isCaught) return;
        _isCaught = true;
        _inputHandler.Block();
        _inputHandler.PlayerInput.DeactivateInput();
        ResetInputState();
        ForceClearInteractionState();
        _physics.ForceStopForce();
        _view.OnStopForce();
        _physics.SetInputDirection(Vector3.zero);
        _physics.SetMoveDirection(Vector3.zero);

        if (_cheatHelper.IsCheating) StopCheating();
        if (IsPeeking) RestoreInputScope(instant: true);
        else if (_cheatHelper.IsPeeking) StopPeeking();

        _view.OnCaught(transform.position, onComplete: () =>
        {
            _isCaught = false;
            _inputHandler.PlayerInput.ActivateInput();
            ResetInputState();
            if (doorPoint != null)
                TeleportToDoor(doorPoint);
            else
                TeleportToInitialChair();
            StartCoroutine(DelayedUnblock());
            onAfterTeleport?.Invoke();
        });
    }
    public void OnGameOver(Action onComplete)
    {
        _inputHandler.PlayerInput.DeactivateInput();
        ResetInputState();
        ForceClearInteractionState();
        _view.OnCaught(transform.position, onComplete: onComplete);
    }
    public void ForceStopStun()
    {
        _stunHelper.ForceStop();
    }

    public void ResetPlayerState()
    {
        _isCaught = false;
        _isWalkingBack = false;

        if (_cheatHelper.IsCheating) StopCheating();
        if (_cheatHelper.IsPeeking) StopPeeking();
        if (_cheatHelper.IsRemembering) _cheatHelper.StopRemembering();
        if (_craftHelper.IsCrafting && _craftHelper.TryStopCraftingItem())
            _view.StopCrafting();

        _view.HideThrowPreview();
        _view.HideLobThrowPreview();
        _view.HideDynamicLobThrowPreview();

        HideAnswerSheet();
        DestroyHeldItem();

        _physics.StopFollowingPath();
        ResetInputState();
        ForceClearInteractionState();
        ForceStopStun();
    }

    private System.Collections.IEnumerator DelayedUnblock()
    {
        yield return null; // wait 1 frame
        yield return null; // wait another frame
        _inputHandler.Unblock();
    }
    public void ForceStopForce()
    {
        _physics.ForceStopForce();
        _view.OnStopForce();
    }
    public void SetAnsweringDuration(float duration)
    {
        if (_answerController != null)
            _answerController.SetDurations(0, duration, 0);
    }
    public void OnCaughtWalkBack(Action onSeated, Vector3? avoidPosition = null)
    {

        if (_isCaught) return;
        _isCaught = true;
        _isWalkingBack = true;

        _view.PlayCaughtSymbols();

        _inputHandler.Block();
        _inputHandler.PlayerInput.DeactivateInput();
        ResetInputState();
        ForceClearInteractionState();
        ForceStopForce();
        if (_cheatHelper.IsCheating) StopCheating();
        if (IsPeeking) RestoreInputScope(instant: true);
        else if (_cheatHelper.IsPeeking) StopPeeking();

        if (_initialChairController.IsBlocked)
            _initialChairController.Unblock();

        bool pathFound = _physics.StartFollowingNavMeshPath(
            _initialChairController.SittingPoint.position,
            avoidPosition
        );

        if (!pathFound)
        {
            // Fallback to teleport if no valid path
            _physics.StopFollowingPath();
            _chairHelper.TeleportToSitting(_initialChairController);
            _isCaught = false;
            _isWalkingBack = false;
            _answerController = _initialChairController.AnswerController;
            _inputHandler.PlayerInput.ActivateInput();
            StartCoroutine(DelayedUnblock());
            onSeated?.Invoke();

            return;
        }
        _onWalkBackSeated = onSeated;

        _walkBackTimeout = StartCoroutine(WalkBackTimeoutRoutine());
        _physics.OnArriveEvent += OnArrivedAtChair;
    }

    public void OnPushedAside(Vector3 direction)
    {
        if (_isCaught) return;

        _physics.ApplyImpulse(direction * _pushForce);
        _stunHelper.StartStun(true);

        // Briefly ignore collision with the walking-back player
        // The collision will re-enable via the existing CollisionComponent or after the stun ends
    }
    public Collider[] GetColliders()
    {
        return _physics.Colliders;
    }
    public void PlayCaughtAudio()
    {
        _audioHelper.OnCaught();
    }

    //refactor to avoid having 2 similar functions
    private void TryShowAnswerSheetOnSit()
    {
        if (_globalDefinition.ShowAnswerSheetOnSit)
            ShowAnswerSheet();
    }

    public AnswerSheet GetAnswerSheet()
    {
        return _answerController?.AnswerSheet;
    }
    public void DestroyHeldItem()
    {
        if (!_interactionHelper.TryGetPickedUpInteraction(out InteractionController interaction))
            return;
        
        _interactionHelper.TryStopInteraction(interaction);
        _view.OnDrop(interaction.transform);
        Destroy(interaction.gameObject);
    }

    private IEnumerator WalkBackTimeoutRoutine()
    {
        yield return new WaitForSeconds(3);
        if (_isWalkingBack)
            CompleteWalkBack();
    }

    private void CompleteWalkBack()
    {
        _physics.OnArriveEvent -= OnArrivedAtChair;
        if (_walkBackTimeout != null)
        {
            StopCoroutine(_walkBackTimeout);
        }

        _view.StopCaughtSymbols();
        _physics.StopFollowingPath();
        _answerController = _initialChairController.AnswerController;
        _chairHelper.TeleportToSitting(_initialChairController);
        _lookHelper.SetLookAt(_initialChairController.LookAtPoint);
        _lookHelper.RestoreInitialLookDirection();
        _isCaught = false;
        _isWalkingBack = false;
        _inputHandler.PlayerInput.ActivateInput();
        StartCoroutine(DelayedUnblock());
        _onWalkBackSeated?.Invoke();

    }
    private void OnArrivedAtChair()
    {
        CompleteWalkBack();
    }
    private bool CanApproachChair(ChairController chair)
    {
        Vector3 toChair = (chair.SittingPoint.position - transform.position).normalized;
        float behindDot = Vector3.Dot(toChair, -chair.transform.forward);

        // behindDot > 0.5 means player is mostly behind the chair
        return behindDot < 0.8f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_physics.IsDashing) return;
        if (_chairHelper.IsSitting) return;
        if (other.gameObject.name != "DashSitTrigger") return;

        ChairController chair = other.GetComponentInParent<ChairController>();
        if (chair == null) return;
        if (!chair.CanPlayerSit) return;
        if (!CanApproachChair(chair)) return;

        ForceStopForce();
        RequestSitting(chair);
    }
}
