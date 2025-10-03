using UnityEngine;

public class DeskController : MonoBehaviour
{
    [SerializeField] private Transform _lookAtPoint;
    [SerializeField] private Transform _sittingPoint;
    [SerializeField] private Transform[] _standingPoints;

    public Transform LookAtPoint => _lookAtPoint;
    public Transform SittingPoint => _sittingPoint;
    public Transform[] StandingPoints => _standingPoints;
}
