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
    [SerializeField] private CheatHelper.Data _cheatData;
    [SerializeField] private MovementHelper.Data _movementData;
    [Header("TO BE REMOVED")]
    [SerializeField] private PaperBallController _answerPrefab;
    [SerializeField] private bool _dropByHoldingInteract; // Once we decide on the final input scheme, this can be removed

    // Helpers
    private InteractionHelper _interactionHelper;
    private ThrowHelper _throwHelper;
    private DeskHelper _deskHelper;
    private StunHelper _stunHelper;
    private CheatHelper _cheatHelper;
    private MovementHelper _movementHelper;

    // IActor
    string IActor.ID => IActor.GetPlayerID(_inputHandler.PlayerInput.playerIndex);
    // IInteractionActor
    Vector3 IInteractionActor.Position => transform.position;
    Vector3 IInteractionActor.Forward => transform.forward;
    // IThrowActor
    Vector3 IThrowActor.LookDirection => _movementHelper.LookDirection;
    Collider[] IThrowActor.Colliders => _physics.Colliders;

    private void Awake()
    {
        _physics.Initialize();
        _interactionHelper = new InteractionHelper(this, _interactionData, isEnabled: true);
        _throwHelper = new ThrowHelper(this, _throwData, _interactionHelper);
        _deskHelper = new DeskHelper(_inputHandler, _view, _physics);
        _stunHelper = new StunHelper(_stunData, _view);
        _cheatHelper = new CheatHelper(_cheatData, _view);
        _movementHelper = new MovementHelper(_view, _physics, _movementData);

        // Initialize
        _movementHelper.Initialize(transform.forward);
        _fieldOfViewController.Hide();
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
            _movementHelper.RequestDash();
        }
        else if (actionType == EAction.Interact)
        {
            if (_cheatHelper.IsCheating)
            {
                StopCheating();
            }
            else if (_deskHelper.IsSitting)
            {
                _deskHelper.HideAnswersSheet();
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
            }
            else if (_inputHandler.ScopeType == EInputScope.PlayerSitting)
            {
                RequestStanding();
            }
        }
        else if (actionType == EAction.Peek)
        {
            if (_inputHandler.ScopeType == EInputScope.PlayerPeeking)
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
                _movementHelper.SetLookAt(chairController.AnswerController.LookAtPoint);
                _deskHelper.StartSitting(chairController);
                _interactionHelper.DisableInteraction();
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
                _movementHelper.SetLookAt(answerController.LookAtPoint);
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

    private void StopPeeking()
    {
        _movementHelper.ClearLookAt();
        _cheatHelper.StopPeeking();
        StopStaticInteraction();
    }

    private void StopCheating()
    {
        _movementHelper.ClearLookAt();
        _cheatHelper.StopCheating();
        StopStaticInteraction();
    }

    private void RequestStanding()
    {
        _movementHelper.ClearLookAt();
        _deskHelper.StartStanding();
        _interactionHelper.EnableInteraction();
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
        if (_deskHelper.IsSitting)
        {
            _movementHelper.SetLookAt(_deskHelper.LookAtPoint);
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
            _physics.SetMoveDirection(new Vector3(input.x, 0, input.y));
            _movementHelper.SetLookInput(input);
        }
        else if (actionType == EDirectionalAction.Aim)
        {
            _movementHelper.SetLookInput(input);
        }
    }

    private void OnPreHoldActionDetected(EAction actionType)
    {
        if (actionType == EAction.Interact)
        {
            if (_deskHelper.IsSitting)
            {
                if (_cheatHelper.TryGetRememberedAnswer(out string answerID))
                {
                    _deskHelper.TryStartAnswering(answerID);
                }
                else if (_interactionHelper.TryGetPickedUpInteraction(out PaperBallController paperBallController) && paperBallController.HasAnswer)
                {
                    _deskHelper.TryStartAnswering(paperBallController.AnswerID);
                }
                else
                {
                    _deskHelper.TryShowAnswersSheet();
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
                _movementHelper.ClearLookAt();
                _inputHandler.SetScope(EInputScope.PlayerAiming);
            }
        }
        else if (actionType == EAction.Peek)
        {
            _movementHelper.ClearLookAt();
            _inputHandler.SetScope(EInputScope.PlayerPeeking);
            _fieldOfViewController.Show();
        }
    }

    private void OnHoldActionRequested(EAction actionType, bool isHolding)
    {
        if (actionType == EAction.Interact)
        {
            if (_deskHelper.IsSitting)
            {
                if (!isHolding)
                {
                    _deskHelper.HideAnswersSheet();
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
        }
        else if (actionType == EAction.Peek)
        {
            if (!isHolding)
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
        }
    }

    private void Update()
    {
        _movementHelper.UpdateCooldown();
        if (_stunHelper.IsStunned)
        {
            _stunHelper.UpdateStun();
            return;
        }

        _movementHelper.UpdateRotation(transform);
        _interactionHelper.UpdateBestInteraction();
        if (_deskHelper.IsAnswering)
        {
            _deskHelper.TryUpdateAnswering(out bool finishedAnswering);
            if (finishedAnswering)
            {
                if (_cheatHelper.TryGetRememberedAnswer(out string answerID))
                {
                    _cheatHelper.StopRemembering();

                    // Create answer
                    PaperBallController answerInstance = Instantiate(_answerPrefab, _view.PickUpPosition + Vector3.up, Quaternion.identity); // Slightly above to highlight briefly.
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
            }
        }
        if (_cheatHelper.IsRemembering && !_deskHelper.IsAnswering)
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
        => _movementHelper.OnCollisionStay(collision, OnStopDash: _stunHelper.StartStun);

    private void OnTriggerEnter(Collider other)
    {
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
        if (!_interactionHelper.TryRemoveInteraction(other, out InteractionController interaction)) return;
        if (_inputHandler.ScopeType != EInputScope.PlayerPeeking) return;

        if (interaction.TryGetComponent(out AnswerController answerController))
        {
            _cheatHelper.StopPeeking();
            _interactionHelper.TryStopInteraction(interaction);
        }
    }

    void IThrowActor.OnThrow(Transform thrownTransform)
        => _view.OnThrow(thrownTransform);
}
