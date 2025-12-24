using UnityEngine;

public class PlayerController : MonoBehaviour, IInteractionActor, IThrowActor
{
    [SerializeField] private PlayerView _view;
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private PlayerPhysics _physics;
    [SerializeField] private FieldOfViewController _fieldOfViewController;

    [Header("Data")]
    [SerializeField] private InteractionHelper.Data _interactionData;
    [SerializeField] private ThrowHelper.Data _throwData;
    [SerializeField] private StunHelper.Data _stunData;
    [SerializeField] private PlayerCheatHelper.Data _cheatData;
    [UnityEngine.Serialization.FormerlySerializedAs("_movementData")]
    [SerializeField] private DashHelper.Data _dashData;
    [SerializeField] private LookHelper.Data _lookData;
    [SerializeField] private PlayerAudioHelper.Data _audioData;
    [Header("TO BE REMOVED")]
    [SerializeField] private bool _dropByHoldingInteract; // Once we decide on the final input scheme, this can be removed
    [SerializeField] private bool _toggleToPeek;

    // Runtime
    private AnswerController _answerController;
    private ChairController _initialChairController;

    private bool IsPeeking => _inputHandler.ScopeType == EInputScope.PlayerPeeking;
    private bool IsAnswering => _answerController != null && _answerController.IsAnswering;

    // Helpers
    private InteractionHelper _interactionHelper;
    private ThrowHelper _throwHelper;
    private StunHelper _stunHelper;
    private PlayerCheatHelper _cheatHelper;
    private LookHelper _lookHelper;
    private DashHelper _dashHelper;
    private ChairHelper _chairHelper;
    private PlayerAudioHelper _audioHelper;

    // IActor
    string IActor.ID => IActor.GetPlayerID(_inputHandler.PlayerInput.playerIndex);
    // IInteractionActor
    Vector3 IInteractionActor.Position => transform.position;
    Vector3 IInteractionActor.Forward => transform.forward;
    // IThrowActor
    Vector3 IThrowActor.LookDirection => _view.transform.forward;
    Collider[] IThrowActor.Colliders => _physics.Colliders;

    private void Awake()
    {
        _physics.Initialize();
        _interactionHelper = new InteractionHelper(_interactionData, this, isEnabled: true);
        _throwHelper = new ThrowHelper(_throwData, this, _interactionHelper);
        _chairHelper = new ChairHelper(_inputHandler, _view, _physics);
        _stunHelper = new StunHelper(_stunData, _view);
        _cheatHelper = new PlayerCheatHelper(_cheatData, _view);
        _lookHelper = new LookHelper(_lookData);
        _audioHelper = new PlayerAudioHelper(_audioData);
        _dashHelper = new DashHelper(_dashData, _view, _physics, _lookHelper, _audioHelper);

        // Initialize
        _lookHelper.Initialize(transform.forward);
        _fieldOfViewController.Hide();
        TeleportToInitialChair();
    }

    private void OnEnable()
    {
        _inputHandler.ActionEvent += OnActionRequested;
        _inputHandler.DirectionalActionEvent += OnDirectionalActionRequested;
        _inputHandler.PreHoldActionEvent += OnPreHoldActionDetected;
        _inputHandler.HoldActionEvent += OnHoldActionRequested;
    }

    private void OnDisable()
    {
        _inputHandler.ActionEvent -= OnActionRequested;
        _inputHandler.DirectionalActionEvent -= OnDirectionalActionRequested;
        _inputHandler.PreHoldActionEvent -= OnPreHoldActionDetected;
        _inputHandler.HoldActionEvent -= OnHoldActionRequested;
    }

    private void OnActionRequested(EAction actionType)
    {
        if (actionType == EAction.Dash)
        {
            _dashHelper.RequestDash();
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

    private void StartAnswering(string answerID)
    {
        if (_answerController.TryStartAnswering(answerID))
        {
            _audioHelper.OnStartAnswering();
        }
    }

    private void HideAnswerSheet()
    {
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

    private void RestoreInputScope()
    {
        // Handle specific cases
        if (_inputHandler.ScopeType == EInputScope.PlayerPeeking)
        {
            StopPeeking();
            _fieldOfViewController.Hide();
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

    private void OnDirectionalActionRequested(EDirectionalAction actionType, Vector2 input)
    {
        if (actionType == EDirectionalAction.Move)
        {
            _physics.SetInputDirection(new Vector3(input.x, 0, input.y), updateMoveDirection: true);
            _lookHelper.SetLookInput(input);
        }
        else if (actionType == EDirectionalAction.Aim)
        {
            _physics.SetInputDirection(new Vector3(input.x, 0, input.y), updateMoveDirection: false);
            _lookHelper.SetLookInput(input);
        }
    }

    private void OnPreHoldActionDetected(EAction actionType)
    {
        if (actionType == EAction.Interact)
        {
            if (_chairHelper.IsSitting)
            {
                if (_cheatHelper.TryGetRememberedAnswer(out string answerID))
                {
                    StartAnswering(answerID);
                }
                else if (_interactionHelper.TryGetPickedUpInteraction(out PaperBallController paperBallController) && paperBallController.HasAnswer)
                {
                    StartAnswering(paperBallController.AnswerID);
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
    }

    private void TryDropItem()
    {
        if (_interactionHelper.TryGetPickedUpInteraction(out InteractionController stoppedInteraction))
        {
            _interactionHelper.TryStopInteraction(stoppedInteraction);
            _view.OnDrop(stoppedInteraction.transform);
            _view.HideThrowPreview();
        }
    }

    private void Update()
    {
        _dashHelper.UpdateCooldown();
        if (_stunHelper.IsStunned)
        {
            _stunHelper.UpdateStun();
            return;
        }

        _lookHelper.UpdateRotation(transform);
        _interactionHelper.UpdateBestInteraction();
        if (IsAnswering)
        {
            _answerController.UpdateAnswering(out bool finishedAnswering);
            if (finishedAnswering)
            {
                _audioHelper.OnFinishedCorrectAnswer();

                if (_cheatHelper.TryGetRememberedAnswer(out string answerID))
                {
                    _cheatHelper.StopRemembering();

                    // Create answer
                    PaperBallController answerInstance = ItemsManager.GetInstance().InstantiateAnswer(_view.PickUpPosition + Vector3.up, Quaternion.identity, parent: null); // Slightly above to highlight briefly.
                    answerInstance.SetAnswer(answerID);
                    _view.OnPickUp(answerInstance.transform);
                    _interactionHelper.AddInteraction(answerInstance.InteractionController);
                    _interactionHelper.StartInteraction(answerInstance.InteractionController);
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
    }

    private void FixedUpdate()
    {
        _physics.OnFixedUpdate(Time.fixedDeltaTime, canMove: !_stunHelper.IsStunned, out bool stoppedDashing);
        if (stoppedDashing)
        {
            _view.OnStopDash();
        }
    }

    private void OnCollisionStay(Collision collision)
        => _dashHelper.OnCollisionStay(collision, OnStopDash: _stunHelper.StartStun);

    private void OnTriggerEnter(Collider other)
    {
        //Debug.LogError("OnTriggerEnter: " + other.name + ", parent: " + other.transform.parent.name, other);
        if (!_interactionHelper.TryAddInteraction(other, out InteractionController interaction)) return;
        if (_inputHandler.ScopeType != EInputScope.PlayerPeeking) return;

        if (interaction.TryGetComponent(out AnswerController answerController) && _cheatHelper.CanStartPeeking(answerController))
        {
            _cheatHelper.StartPeeking(answerController);
            _interactionHelper.StartInteraction(interaction);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.LogError("OnTriggerExit: " + other.name + ", parent: " + other.transform.parent.name, other);
        if (!_interactionHelper.TryRemoveInteraction(other, out InteractionController interaction)) return;
        if (_inputHandler.ScopeType != EInputScope.PlayerPeeking) return;

        if (interaction.TryGetComponent(out AnswerController answerController) && _cheatHelper.CanStopPeeking(answerController))
        {
            _cheatHelper.StopPeeking();
            _interactionHelper.TryStopInteraction(interaction);
        }
    }

    void IThrowActor.OnThrow(Transform thrownTransform)
        => _view.OnThrow(thrownTransform);
}
