using System;
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
    [SerializeField] private StunHelper.Data _stunData;
    [SerializeField] private PlayerCheatHelper.Data _cheatData;
    [SerializeField] private DashHelper.Data _dashData;
    [SerializeField] private LookHelper.Data _lookData;
    [SerializeField] private PlayerAudioHelper.Data _audioData;
    [SerializeField] private GlobalDefinition _globalDefinition;
    [Header("TO BE REMOVED")]
    [SerializeField] private bool _dropByHoldingInteract; // Once we decide on the final input scheme, this can be removed
    [SerializeField] private bool _toggleToPeek;
    [SerializeField] private bool _stopPeekOnDash;
    [SerializeField] private bool _stopPeekOnTeleport;

    // Runtime
    private AnswerController _answerController;
    private ChairController _initialChairController;

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
        _craftHelper = new CraftHelper(_view, _interactionHelper, GameContext.ItemsManager);

        // Initialize
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
                RestoreInputScope();
                _inputHandler.CancelActionHold();
                _view.HideThrowPreview();
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
            // TODO: Hide inventory
            _craftHelper.TryStopCraftingItem();
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
            if (chairController.CanPlayerSit)
            {
                RequestSitting(chairController);
            }
            return true;
        }

        if (interaction.TryGetComponent<AnswerController>(out _))
        {
            // Empty for now
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
            _audioHelper.OnStartAnswering();
        }
    }

    private void HideAnswerSheet()
    {
        if (_answerController == null)
            return;

        _answerController.HideAnswerSheet();
        _audioHelper.TryStopAnswering();
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
            Debug.LogError("Initial chair controller is blocked");
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
        _lookHelper.ClearLookAt();
        _chairHelper.StartStanding();
        _interactionHelper.EnableInteraction();
        if (_answerController != null)
        {
            _answerController.HideAnswerSheet();
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
                if (_cheatHelper.TryGetRememberedAnswer(out string answerID, out float correctness, out string actorID) && CanStartAnswering(answerID, correctness, actorID, sourceID: null))
                {
                    StartAnswering(answerID, correctness, actorID, sourceID: null); // Source is null because it's answered from memory and we already pass the actorID.
                }
                else if (_interactionHelper.TryGetPickedUpInteraction(out PaperBallController paperBallController) && paperBallController.HasAnswer && CanStartAnswering(paperBallController.AnswerID, paperBallController.Correctness, paperBallController.ContributorActorID, paperBallController.ID))
                {
                    StartAnswering(paperBallController.AnswerID, paperBallController.Correctness, paperBallController.ContributorActorID, paperBallController.ID);
                }
                else
                {
                    _answerController.ShowAnswerSheet();
                }
            }
            else
            {
                TryStartInteractionOnHold();
            }
        }
        else if (actionType == EAction.Action)
        {
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
            // TODO: Show inventory
            if (!_interactionHelper.TryGetPickedUpInteraction(out _))
            {
                _craftHelper.TryStartCraftingItem("Paper Ball");
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
                    HideAnswerSheet();
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
            if (!isHolding)
            {
                RestoreInputScope();
                _throwHelper.TryTriggerThrow();
            }
            else if (_throwHelper.CanShowPreview())
            {
                _view.ShowThrowPreview();
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
                // TODO: Hide inventory
                _craftHelper.TryStopCraftingItem();
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
        }
    }

    private void Update()
    {
        if (!GameManager.Instance.GameplayActive)
            return;

        _dashHelper.UpdateCooldown();
        if (_stunHelper.IsStunned)
        {
            _stunHelper.UpdateStun();
            return;
        }

        if (IsPeeking && _lastAimInput.IsMouse)
        {
            ProcessAimInput(_lastAimInput.Input, _lastAimInput.IsMouse); // [AKP] Force aim to stick on current mouse position while moving
        }

        _lookHelper.UpdateRotation(transform);
        _interactionHelper.UpdateBestInteraction();
        if (IsAnswering)
        {
            _answerController.UpdateAnswering(Time.deltaTime, out bool finishedAnswering);
            if (finishedAnswering)
            {
                _answerController.StartIdle();
                _audioHelper.OnFinishedCorrectAnswer();
                if (_cheatHelper.TryGetRememberedAnswer(out string answerID, out float correctness, out string actorID))
                {
                    _cheatHelper.StopRemembering();
                    PaperBallController answerInstance = _craftHelper.CraftAnswer(answerID, correctness, actorID);
                    _answerController.AddContributor(answerID, answerInstance.ID);
                }
            }
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
        if (_craftHelper.IsCrafting)
        {
            _craftHelper.UpdateCrafting(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (!GameManager.Instance.GameplayActive)
            return;

        _physics.OnFixedUpdate(Time.fixedDeltaTime, canMove: !_stunHelper.IsStunned, out bool stoppedDashing);
        if (stoppedDashing)
        {
            _view.OnStopDash();
        }
    }

    private void OnCollisionStay(Collision collision)
        => _dashHelper.OnCollisionStay(collision, onStopDash: _stunHelper.StartStun);

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
        if (_inputHandler.ScopeType != EInputScope.PlayerPeeking) return;
        if (!_interactionHelper.TryGetInteraction(other, out InteractionController interaction)) return;

        if (interaction.TryGetComponent(out AnswerController answerController) && _cheatHelper.CanStartPeeking(answerController))
        {
            _cheatHelper.StartPeeking(answerController);
            _interactionHelper.StartInteraction(interaction);
        }
    }

    private void OnFovTriggerExit(Collider other)
    {
        if (_inputHandler.ScopeType != EInputScope.PlayerPeeking) return;
        if (!_interactionHelper.TryGetInteraction(other, out InteractionController interaction)) return;

        if (interaction.TryGetComponent(out AnswerController answerController) && _cheatHelper.CanStopPeeking(answerController))
        {
            _cheatHelper.StopPeeking();
            _interactionHelper.TryStopInteraction(interaction);
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
}
