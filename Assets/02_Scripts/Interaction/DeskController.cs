using System.Collections.Generic;
using UnityEngine;

public class DeskController : MonoBehaviour
{
    [SerializeField] private Transform _lookAtPoint;
    [SerializeField] private Transform _sittingPoint;
    [SerializeField] private Transform[] _standingPoints;
    [SerializeField] private AnswersSheetUI _answersSheetUI;

    public Transform LookAtPoint => _lookAtPoint;
    public Transform SittingPoint => _sittingPoint;
    public Transform[] StandingPoints => _standingPoints;

    private void Awake()
    {
        _answersSheetUI.Hide();
    }

    public void ShowAnswersSheet()
    {
        _answersSheetUI.Show();
    }

    public void HideAnswersSheet()
    {
        _answersSheetUI.Hide();
    }

    public void SetupAnswersSheet(bool[] answers)
    {
        _answersSheetUI.Setup(answers);
    }
}
