using UnityEngine;

public class ChairController : MonoBehaviour
{
    [SerializeField] private InteractionController _interactionController;
    [SerializeField] private Transform _sittingPoint;
    [SerializeField] private Transform[] _standingPoints;

    private bool _isBlocked;

    public DeskController DeskController { get; private set; }
    public Transform SittingPoint => _sittingPoint;
    public Transform[] StandingPoints => _standingPoints;

    public bool CanPlayerSit => !_isBlocked && DeskController != null && DeskController.IsPlayerDesk;

    public void Setup(DeskController deskController)
    {
        DeskController = deskController;
        if (!deskController.IsPlayerDesk)
        {
            _interactionController.Disable();
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
