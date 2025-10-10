using UnityEngine;

public class AnswersManager : MonoBehaviour
{
    [SerializeField] private DeskController.Data _data = new DeskController.Data() { AnswersCount = 10, AnswerDuration = 1.5f };
    [SerializeField] private DeskController[] _playerDesks;
    [SerializeField] private DeskController[] _desksWithAnswersSheet;

    private void Awake()
    {
        for (int i = 0; i < _playerDesks.Length; i++)
        {
            int playerIndex = i;
            _playerDesks[i].Setup(_data, playerIndex);
        }
        foreach (var deskController in _desksWithAnswersSheet)
        {
            deskController.Setup(_data, playerIndex: -1);
        }
    }
}
