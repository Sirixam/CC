using UnityEngine;
using UnityEngine.InputSystem;

public enum EInputScope
{
    Undefined,
    Menu,
    PlayerStanding,
    PlayerSitting,
    PlayerAiming,
    PlayerPeeking,
}

public partial class PlayerInputHandler
{
    /// <summary>
    /// This class handles when inputs should be active via scopes
    /// </summary>
    public class Mapper
    {
        private const string PLAYER_STANDING_MAP = "Player - Standing";
        private const string PLAYER_SITTING_MAP = "Player - Sitting";
        private const string PLAYER_AIMING_MAP = "Player - Aiming";
        private const string PLAYER_PEEKING_MAP = "Player - Peeking";
        //private const string MENU_MAP = "Menu";

        private static readonly string AIM_ACTION = EDirectionalAction.Aim.ToString(); // AimMouse use the same Action
        private static readonly string MOVE_ACTION = EDirectionalAction.Move.ToString();
        private static readonly string NAVIGATE_ACTION = EDirectionalAction.Navigate.ToString();
        private static readonly string ACTION_ACTION = EAction.Action.ToString();
        private static readonly string INTERACT_ACTION = EAction.Interact.ToString();
        private static readonly string UTILITY_ACTION = EAction.Utility.ToString();
        private static readonly string DASH_ACTION = EAction.Dash.ToString();
        private static readonly string PEEK_ACTION = EAction.Peek.ToString();
        private static readonly string CANCEL_ACTION = EAction.Cancel.ToString();
        private static readonly string PAUSE_ACTION = EAction.Pause.ToString();

        private PlayerInputHandler _inputHandler;
        private EInputScope _nextScopeType; // Used when changing scope.

        private PlayerInput PlayerInput => _inputHandler.PlayerInput;
        public EInputScope ScopeType { get; private set; }

        public Mapper(PlayerInputHandler inputHandler)
        {
            _inputHandler = inputHandler;

            // DO NOT iterate over all action maps, because we want to keep some enabled, eg UI
            PlayerInput.actions.FindActionMap(PLAYER_STANDING_MAP).Disable();
            PlayerInput.actions.FindActionMap(PLAYER_SITTING_MAP).Disable();
            PlayerInput.actions.FindActionMap(PLAYER_AIMING_MAP).Disable();
            PlayerInput.actions.FindActionMap(PLAYER_PEEKING_MAP).Disable();
            //_playerInput.actions.FindActionMap(MENU_MAP).Disable();

            Debug.Log("Current control scheme: " + PlayerInput.currentControlScheme);
        }

        public void OnEnable()
        {
            if (ScopeType != EInputScope.Undefined)
            {
                SubscribeActions(ScopeType);
            }
        }

        public void OnDisable()
        {
            if (ScopeType != EInputScope.Undefined)
            {
                UnsubscribeActions(ScopeType);
            }
        }

        public void SetScope(EInputScope scopeType)
        {
            if (ScopeType == scopeType) return;

            _nextScopeType = scopeType;
            UnsubscribeActions(ScopeType);
            ScopeType = scopeType;
            SubscribeActions(scopeType);
            _nextScopeType = EInputScope.Undefined;
        }

        #region SCOPE

        // Supress cancel when changing scope to one that has the input too.
        public bool SupressCancelOnScopeChange(EAction action)
        {
            if (_nextScopeType == EInputScope.Undefined) return false;
            switch (action)
            {
                case EAction.Action: return HasActionInput(_nextScopeType);
                case EAction.Peek: return HasPeekInput(_nextScopeType);
                default:
                    return false;
            }
        }

        // EXTENDABLE
        private bool HasActionInput(EInputScope scopeType)
        {
            return scopeType == EInputScope.PlayerStanding || scopeType == EInputScope.PlayerSitting || scopeType == EInputScope.PlayerAiming;
        }

        private bool HasPeekInput(EInputScope scopeType)
        {
            return scopeType == EInputScope.PlayerStanding || scopeType == EInputScope.PlayerPeeking;
        }

        private void UnsubscribeActions(EInputScope scopeType)
        {
            switch (scopeType)
            {
                case EInputScope.Menu: UnsubscribeMenuActions(); break;
                case EInputScope.PlayerStanding: UnsubscribePlayerStandingActions(); break;
                case EInputScope.PlayerSitting: UnsubscribePlayerSittingActions(); break;
                case EInputScope.PlayerAiming: UnsubscribePlayerAimingActions(); break;
                case EInputScope.PlayerPeeking: UnsubscribePlayerPeekingActions(); break;
                case EInputScope.Undefined: break;
                default:
                    Debug.LogError("Scope is not being handled: " + scopeType);
                    break;
            }
        }

        private void SubscribeActions(EInputScope scopeType)
        {
            switch (scopeType)
            {
                case EInputScope.Menu: SubscribeMenuActions(); break;
                case EInputScope.PlayerStanding: SubscribePlayerStandingActions(); break;
                case EInputScope.PlayerSitting: SubscribePlayerSittingActions(); break;
                case EInputScope.PlayerAiming: SubscribePlayerAimingActions(); break;
                case EInputScope.PlayerPeeking: SubscribePlayerPeekingActions(); break;
                default:
                    Debug.LogError("Scope is not being handled: " + scopeType);
                    break;
            }
        }

        private void SubscribePlayerStandingActions()
        {
            PlayerInput.actions.FindActionMap(PLAYER_STANDING_MAP).Enable();

            var actions = PlayerInput.actions;
            actions[MOVE_ACTION].started += _inputHandler.OnMove;
            actions[MOVE_ACTION].performed += _inputHandler.OnMove;
            actions[MOVE_ACTION].canceled += _inputHandler.OnMove;

            actions[ACTION_ACTION].started += _inputHandler.OnAction;
            actions[ACTION_ACTION].performed += _inputHandler.OnAction;
            actions[ACTION_ACTION].canceled += _inputHandler.OnAction;

            actions[INTERACT_ACTION].started += _inputHandler.OnInteract;
            actions[INTERACT_ACTION].performed += _inputHandler.OnInteract;
            actions[INTERACT_ACTION].canceled += _inputHandler.OnInteract;

            actions[PEEK_ACTION].started += _inputHandler.OnPeek;
            actions[PEEK_ACTION].performed += _inputHandler.OnPeek;
            actions[PEEK_ACTION].canceled += _inputHandler.OnPeek;

            actions[DASH_ACTION].performed += _inputHandler.OnDash;
            actions[PAUSE_ACTION].performed += _inputHandler.OnPause;
        }

        private void UnsubscribePlayerStandingActions()
        {
            PlayerInput.actions.FindActionMap(PLAYER_STANDING_MAP).Disable();

            var actions = PlayerInput.actions;
            actions[MOVE_ACTION].started -= _inputHandler.OnMove;
            actions[MOVE_ACTION].performed -= _inputHandler.OnMove;
            actions[MOVE_ACTION].canceled -= _inputHandler.OnMove;

            actions[ACTION_ACTION].started -= _inputHandler.OnAction;
            actions[ACTION_ACTION].performed -= _inputHandler.OnAction;
            actions[ACTION_ACTION].canceled -= _inputHandler.OnAction;

            actions[INTERACT_ACTION].started -= _inputHandler.OnInteract;
            actions[INTERACT_ACTION].performed -= _inputHandler.OnInteract;
            actions[INTERACT_ACTION].canceled -= _inputHandler.OnInteract;

            actions[PEEK_ACTION].started -= _inputHandler.OnPeek;
            actions[PEEK_ACTION].performed -= _inputHandler.OnPeek;
            actions[PEEK_ACTION].canceled -= _inputHandler.OnPeek;

            actions[DASH_ACTION].performed -= _inputHandler.OnDash;
            actions[PAUSE_ACTION].performed -= _inputHandler.OnPause;
        }

        private void SubscribePlayerSittingActions()
        {
            PlayerInput.actions.FindActionMap(PLAYER_SITTING_MAP).Enable();

            var actions = PlayerInput.actions;
            actions[NAVIGATE_ACTION].performed += _inputHandler.OnNavigate;
            actions[NAVIGATE_ACTION].canceled += _inputHandler.OnNavigate;

            actions[ACTION_ACTION].started += _inputHandler.OnAction;
            actions[ACTION_ACTION].performed += _inputHandler.OnAction;
            actions[ACTION_ACTION].canceled += _inputHandler.OnAction;

            actions[INTERACT_ACTION].started += _inputHandler.OnInteract;
            actions[INTERACT_ACTION].performed += _inputHandler.OnInteract;
            actions[INTERACT_ACTION].canceled += _inputHandler.OnInteract;

            actions[UTILITY_ACTION].started += _inputHandler.OnUtility;
            actions[UTILITY_ACTION].performed += _inputHandler.OnUtility;
            actions[UTILITY_ACTION].canceled += _inputHandler.OnUtility;

            actions[CANCEL_ACTION].performed += _inputHandler.OnCancel;
            actions[PAUSE_ACTION].performed += _inputHandler.OnPause;
        }

        private void UnsubscribePlayerSittingActions()
        {
            PlayerInput.actions.FindActionMap(PLAYER_SITTING_MAP).Disable();

            var actions = PlayerInput.actions;
            actions[NAVIGATE_ACTION].performed -= _inputHandler.OnNavigate;
            actions[NAVIGATE_ACTION].canceled -= _inputHandler.OnNavigate;

            actions[ACTION_ACTION].started -= _inputHandler.OnAction;
            actions[ACTION_ACTION].performed -= _inputHandler.OnAction;
            actions[ACTION_ACTION].canceled -= _inputHandler.OnAction;

            actions[INTERACT_ACTION].started -= _inputHandler.OnInteract;
            actions[INTERACT_ACTION].performed -= _inputHandler.OnInteract;
            actions[INTERACT_ACTION].canceled -= _inputHandler.OnInteract;

            actions[UTILITY_ACTION].started -= _inputHandler.OnUtility;
            actions[UTILITY_ACTION].performed -= _inputHandler.OnUtility;
            actions[UTILITY_ACTION].canceled -= _inputHandler.OnUtility;

            actions[CANCEL_ACTION].performed -= _inputHandler.OnCancel;
            actions[PAUSE_ACTION].performed -= _inputHandler.OnPause;
        }

        private void SubscribePlayerAimingActions()
        {
            PlayerInput.actions.FindActionMap(PLAYER_AIMING_MAP).Enable();

            var actions = PlayerInput.actions;
            actions[AIM_ACTION].performed += _inputHandler.OnAim;
            actions[AIM_ACTION].canceled += _inputHandler.OnAim;

            actions[ACTION_ACTION].started += _inputHandler.OnAction;
            actions[ACTION_ACTION].canceled += _inputHandler.OnAction;

            actions[CANCEL_ACTION].performed += _inputHandler.OnCancel;
            actions[PAUSE_ACTION].performed += _inputHandler.OnPause;
        }

        private void UnsubscribePlayerAimingActions()
        {
            PlayerInput.actions.FindActionMap(PLAYER_AIMING_MAP).Disable();

            var actions = PlayerInput.actions;
            actions[AIM_ACTION].performed -= _inputHandler.OnAim;
            actions[AIM_ACTION].canceled -= _inputHandler.OnAim;

            actions[ACTION_ACTION].started -= _inputHandler.OnAction;
            actions[ACTION_ACTION].canceled -= _inputHandler.OnAction;

            actions[CANCEL_ACTION].performed -= _inputHandler.OnCancel;
            actions[PAUSE_ACTION].performed -= _inputHandler.OnPause;
        }

        private void SubscribePlayerPeekingActions()
        {
            PlayerInput.actions.FindActionMap(PLAYER_PEEKING_MAP).Enable();

            var actions = PlayerInput.actions;
            actions[AIM_ACTION].performed += _inputHandler.OnAim;
            actions[AIM_ACTION].canceled += _inputHandler.OnAim;

            actions[PEEK_ACTION].started += _inputHandler.OnPeek;
            actions[PEEK_ACTION].performed += _inputHandler.OnPeek;
            actions[PEEK_ACTION].canceled += _inputHandler.OnPeek;

            actions[DASH_ACTION].performed += _inputHandler.OnDash;
            actions[PAUSE_ACTION].performed += _inputHandler.OnPause;
        }

        private void UnsubscribePlayerPeekingActions()
        {
            PlayerInput.actions.FindActionMap(PLAYER_PEEKING_MAP).Disable();

            var actions = PlayerInput.actions;
            actions[AIM_ACTION].performed -= _inputHandler.OnAim;
            actions[AIM_ACTION].canceled -= _inputHandler.OnAim;

            actions[PEEK_ACTION].started -= _inputHandler.OnPeek;
            actions[PEEK_ACTION].performed -= _inputHandler.OnPeek;
            actions[PEEK_ACTION].canceled -= _inputHandler.OnPeek;

            actions[DASH_ACTION].performed -= _inputHandler.OnDash;
            actions[PAUSE_ACTION].performed -= _inputHandler.OnPause;
        }

        private void SubscribeMenuActions()
        {
            //_playerInput.actions.FindActionMap(MENU_MAP).Enable();

            //var actions = _playerInput.actions;
            //actions[NAVIGATE_ACTION].performed += _inputHandler.OnNavigate;
            //actions[NAVIGATE_ACTION].canceled += _inputHandler.OnNavigate;

            // TODO
        }

        private void UnsubscribeMenuActions()
        {
            //_playerInput.actions.FindActionMap(MENU_MAP).Disable();

            //var actions = _playerInput.actions;
            //actions[NAVIGATE_ACTION].performed -= _inputHandler.OnNavigate;
            //actions[NAVIGATE_ACTION].canceled -= _inputHandler.OnNavigate;

            // TODO
        }

        #endregion
    }
}