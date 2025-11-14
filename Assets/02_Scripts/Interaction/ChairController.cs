using UnityEngine;

public class ChairController : MonoBehaviour
{
    [SerializeField] private InteractionController _interactionController;
    [SerializeField] private Transform _sittingPoint;
    [SerializeField] private Transform[] _standingPoints;
    [SerializeField] private GlobalDefinition _globalDefinition;

    private bool _isBlocked;

    public AnswerController AnswerController { get; private set; }
    public Transform SittingPoint => _sittingPoint;
    public Transform[] StandingPoints => _standingPoints;

    public bool CanPlayerSit => !_isBlocked && AnswerController != null && AnswerController.IsPlayer;

    private void Awake()
    {
        AnswerController = transform.parent.GetComponentInChildren<AnswerController>();
    }

    private void Start()
    {
        if (!AnswerController.IsPlayer)
        {
            _interactionController.Disable();
        }
        else if (!_globalDefinition.CanUseAnyPlayerChair)
        {
            _interactionController.AddPlayerToWhitelist(AnswerController.PlayerIndex);
        }
    }

    public void Block()
    {
        _isBlocked = true;
    }

    public void Unblock()
    {
        _isBlocked = false;
    }
}
