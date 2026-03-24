using System;
using System.Collections.Generic;
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
    [Header("Peek")]
    [SerializeField] private float _peekMoveSpeedMultiplier = 0.5f;
    [Header("Push")]
    [SerializeField] private float _pushForce = 5f;
    [Header("TO BE REMOVED")]
    [SerializeField] private bool _dropByHoldingInteract; // Once we decide on the final input scheme, this can be removed
    [SerializeField] private bool _toggleToPeek;
    [SerializeField] private bool _stopPeekOnDash;
    [SerializeField] private bool _stopPeekOnTeleport;
    [Tooltip("If TRUE penalty will apply while peek mode is active, if FALSE it will apply only while peeking a student.")]
    [SerializeField] private bool _applyMovePenaltyOnPeekMode;
    private bool _isCaught;
    public bool IsCaught => _isCaught;

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
            bool hasMemory = _cheatHelper.TryGetRememberedAnswer(out _, out _, out _);

            if (!hasMemory)
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
            //If writing a cheated answer, scale deltaTime to match AnswerDuration
            // instead of the answer's BaseAnswerDuration
            float deltaTime = Time.deltaTime;
            if (_cheatHelper.IsRemembering)
            {
                float baseAnswerDuration = _answerController.GetAnsweringDuration();
                deltaTime *= baseAnswerDuration / _cheatData.AnswerDuration;
            }

            _answerController.UpdateAnswering(deltaTime, out bool finishedAnswering);

            if (finishedAnswering)
            {
                _answerController.StartIdle();
                _audioHelper.OnFinishedCorrectAnswer();
                if (_cheatHelper.TryGetRememberedAnswer(out string answerID, out float correctness, out string actorID))
                {
                    if (!_interactionHelper.TryGetPickedUpInteraction(out _))
                    {
                        _cheatHelper.StopRemembering();
                        PaperBallController answerInstance = _craftHelper.CraftAnswer(answerID, correctness, actorID);
                        _answerController.AddContributor(answerID, answerInstance.ID);
                    }
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

        bool canMove = !_stunHelper.IsStunned && !_isCaught;
        _physics.OnFixedUpdate(Time.fixedDeltaTime, canMove: !_stunHelper.IsStunned && (!_isCaught || _isWalkingBack), out bool stoppedDashing);
        if (stoppedDashing)
        {
            _view.OnStopDash();
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

        // Dash-to-sit logic
        if (!_physics.IsDashing) return;
        if (_chairHelper.IsSitting) return;

        ChairController chair = collision.gameObject.GetComponentInParent<ChairController>();
        if (chair == null) return;
        if (!chair.CanPlayerSit) return;

        // Check approach angle — reject from behind
        Vector3 toChair = (chair.SittingPoint.position - transform.position).normalized;
        Vector3 chairRight = chair.transform.right;
        float sideAlignment = Mathf.Abs(Vector3.Dot(toChair, chairRight));

        // sideAlignment > 0.3 means approaching from the sides
        // Also allow from the front (dot with chair.forward)
        float frontAlignment = Vector3.Dot(toChair, chair.transform.forward);

        if (sideAlignment < 0.3f && frontAlignment < 0.3f)
            return; // approaching from behind, don't allow

        // Stop dash and sit instantly
        ForceStopDash();
        RequestSitting(chair);
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
        _interactionsInsideFoV.Clear();
    }
    private bool IsHoldingItem()
    {
        return _interactionHelper.TryGetPickedUpInteraction(out _);
    }

    public void OnCaught(Action onAfterTeleport)
    {
        if (_isCaught) return;
        _isCaught = true;
        _inputHandler.Block();
        _inputHandler.PlayerInput.DeactivateInput();
        ResetInputState();
        ForceClearInteractionState();
        _physics.ForceStopDash();
        _view.OnStopDash();
        _physics.SetInputDirection(Vector3.zero);
        _physics.SetMoveDirection(Vector3.zero);

        if (_cheatHelper.IsCheating) StopCheating();
        if (_cheatHelper.IsPeeking) StopPeeking();

        _view.OnCaught(transform.position, onComplete: () =>
        {
            _isCaught = false;
            _inputHandler.PlayerInput.ActivateInput();
            ResetInputState();
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
    public void ForceStopDash()
    {
        _physics.ForceStopDash();
        _view.OnStopDash();
    }
    public void SetAnsweringDuration(float duration)
    {
        if (_answerController != null)
            _answerController.SetDurations(0, duration, 0);
    }
    public void OnCaughtWalkBack(Action onSeated, Vector3? avoidPosition = null)
    {
        Debug.Log($"OnCaughtWalkBack called, _isCaught: {_isCaught}");

        if (_isCaught) return;
        _isCaught = true;
        _isWalkingBack = true;

        _view.PlayCaughtSymbols();

        _inputHandler.Block();
        _inputHandler.PlayerInput.DeactivateInput();
        ResetInputState();
        ForceClearInteractionState();
        ForceStopDash();

        if (_cheatHelper.IsCheating) StopCheating();
        if (_cheatHelper.IsPeeking) StopPeeking();

        if (_initialChairController.IsBlocked)
            _initialChairController.Unblock();

        bool pathFound = _physics.StartFollowingNavMeshPath(
            _initialChairController.SittingPoint.position,
            avoidPosition
        );

        Debug.Log($"Path found: {pathFound}");

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
        _physics.OnArriveEvent += OnArrivedAtChair;

        void OnArrivedAtChair()
        {
            _view.PlayCaughtSymbols();
            _physics.OnArriveEvent -= OnArrivedAtChair;
            _view.StopCaughtSymbols();
            _physics.StopFollowingPath();
            _chairHelper.TeleportToSitting(_initialChairController);
            _lookHelper.SetLookAt(_initialChairController.LookAtPoint);
            _lookHelper.RestoreInitialLookDirection();
            _isCaught = false;
            _isWalkingBack = false;
            _answerController = _initialChairController.AnswerController;
            _inputHandler.PlayerInput.ActivateInput();
            StartCoroutine(DelayedUnblock());
            onSeated?.Invoke();
        }
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
}
