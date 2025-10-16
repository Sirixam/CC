using UnityEngine;

public class AnswersManager : MonoBehaviour
{
    [SerializeField] private DeskController.Data _data = new DeskController.Data() { AnswersCount = 10, AnswerDuration = 1.5f };
    [SerializeField] private DeskController[] _playerDesks;
    [SerializeField] private DeskController[] _desksWithAnswersSheet;
    [SerializeField] private GameObject _victoryFeedback;
    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private bool _canUseAnyPlayerChair;

    private void Awake()
    {
        if (_victoryFeedback != null)
        {
            _victoryFeedback.SetActive(false);
        }
        for (int i = 0; i < _playerDesks.Length; i++)
        {
            int playerIndex = i;
            _playerDesks[i].OnFinishAnsweringEvent += OnFinishAnswering;
            _playerDesks[i].Setup(_data, playerIndex, _canUseAnyPlayerChair);
        }
        foreach (var deskController in _desksWithAnswersSheet)
        {
            deskController.OnFinishAnsweringEvent += OnFinishAnswering;
            deskController.Setup(_data, playerIndex: -1, false);
        }
    }

    private void OnFinishAnswering(DeskController deskController)
    {
        if (deskController.IsPlayerDesk && AreAllPlayerAnswersFull())
        {
            _timeManager.Pause();
            if (_victoryFeedback != null)
            {
                _victoryFeedback.SetActive(true);
            }
        }
    }

    private bool AreAllPlayerAnswersFull()
    {
        foreach (var deskController in _playerDesks)
        {
            if (deskController.GetFullAnswersCount() < _data.AnswersCount)
            {
                return false;
            }
        }
        return true;
    }
}
