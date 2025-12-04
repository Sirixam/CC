using System;
using UnityEngine;

public class ChairController : MonoBehaviour
{
    [SerializeField] private InteractionController _interactionController;
    [SerializeField] private Transform _sittingPoint;
    [SerializeField] private Transform[] _standingPoints;
    [SerializeField] private Transform _lookAtPoint;
    [SerializeField] private GlobalDefinition _globalDefinition;

    public AnswerController AnswerController { get; private set; }
    public Transform SittingPoint => _sittingPoint;
    public Transform[] StandingPoints => _standingPoints;
    public Transform LookAtPoint => _lookAtPoint;

    public bool IsBlocked { get; private set; }
    public bool CanPlayerSit => !IsBlocked && AnswerController != null && AnswerController.IsPlayer;

    public Action<Collision> OnCollisionEnterEvent;

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
            _interactionController.AddPlayerToWhitelist(AnswerController.ActorID);
        }
    }

    public void Block()
    {
        IsBlocked = true;
    }

    public void Unblock()
    {
        IsBlocked = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnCollisionEnterEvent?.Invoke(collision);
    }
}
