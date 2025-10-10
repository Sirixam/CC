using UnityEngine;

public class ChairController : MonoBehaviour
{
    [SerializeField] private DeskController _deskController;
    [SerializeField] private Transform _sittingPoint;
    [SerializeField] private Transform[] _standingPoints;

    public DeskController DeskController => _deskController;
    public Transform SittingPoint => _sittingPoint;
    public Transform[] StandingPoints => _standingPoints;
}
