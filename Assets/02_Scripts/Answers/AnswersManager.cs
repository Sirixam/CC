using UnityEngine;

public class AnswersManager : MonoBehaviour
{
    [SerializeField] private DeskController.Data _data = new DeskController.Data() { AnswersCount = 10, AnswerDuration = 1.5f };
    [SerializeField] private DeskController[] _desksWithAnswersSheet;

    private void Awake()
    {
        foreach (var deskController in _desksWithAnswersSheet)
        {
            deskController.Setup(_data);
        }
    }
}
