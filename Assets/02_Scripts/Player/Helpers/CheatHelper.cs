using System;
using UnityEngine;

public class CheatHelper
{
    [Serializable]
    public class Data
    {
        public float PeekDuration;
        public float CheatDuration;
        public float MemoryDuration;
    }

    private Data _data;
    private PlayerView _playerView;
    private AnswerController _answerController;

    private float _peekingProgress;
    private float _cheatingProgress;
    private float _memoryProgress;
    private int _rememberedAnswerNumber;

    public bool IsPeeking { get; private set; }
    public bool IsCheating { get; private set; }
    public bool IsRemembering => _memoryProgress > 0;

    public CheatHelper(Data data, PlayerView playerView)
    {
        _data = data;
        _playerView = playerView;
    }

    public bool CanStartPeeking(AnswerController answerController)
    {
        return answerController.IsAnswering || answerController.IsCheckingAnswer;
    }

    public bool CanStartCheating(AnswerController answerController)
    {
        return answerController.IsCheckingAnswer;
    }

    public void StartPeeking(AnswerController answerController)
    {
        _answerController = answerController;
        IsPeeking = true;
        _peekingProgress = 0;
        _playerView.PeekUI.Show();
        _playerView.PeekUI.SetPercent(_peekingProgress);
    }

    public void StartCheating(AnswerController answerController)
    {
        _answerController = answerController;
        IsCheating = true;
        _cheatingProgress = 0;
        _playerView.CheatUI.Show();
        _playerView.CheatUI.SetPercent(_cheatingProgress);
    }

    public void StartRemembering(int answerNumber, Sprite answerTypeIcon)
    {
        _memoryProgress = 1;
        _rememberedAnswerNumber = answerNumber;
        _playerView.MemoryUI.Show();
        _playerView.MemoryUI.SetAnswerTypeIcon(answerTypeIcon);
        _playerView.MemoryUI.SetAnswerNumber(answerNumber);
        _playerView.MemoryUI.SetPercent(_memoryProgress);
    }

    public void StopPeeking()
    {
        //_deskController?.HideAnswersSheet();
        _answerController = null;
        IsPeeking = false;
        _playerView.PeekUI.Hide();
    }

    public void StopCheating()
    {
        //_deskController?.HideAnswersSheet();
        _answerController = null;
        IsCheating = false;
        _playerView.CheatUI.Hide();
    }

    public void StopRemembering()
    {
        _memoryProgress = 0;
        _rememberedAnswerNumber = 0;
        _playerView.MemoryUI.Hide();
    }

    public void UpdatePeeking(out bool finished)
    {
        float progressDelta = Time.deltaTime / _data.PeekDuration;
        _peekingProgress = Mathf.Clamp01(_peekingProgress + progressDelta);
        finished = _peekingProgress >= 1;
        _playerView.PeekUI.SetPercent(_peekingProgress);

        if (finished)
        {
            _answerController.TriggerFinishedPeeking();
        }
    }

    public void UpdateCheating(out bool finished)
    {
        float progressDelta = Time.deltaTime / _data.CheatDuration;
        _cheatingProgress = Mathf.Clamp01(_cheatingProgress + progressDelta);
        finished = _cheatingProgress >= 1;
        _playerView.CheatUI.SetPercent(_cheatingProgress);

        if (finished)
        {
            Sprite answerTypeIcon = AnswersManager.GetInstance().GetAnswerTypeIcon(_answerController.LastFinishedAnswerNumber);
            StartRemembering(answerNumber: _answerController.LastFinishedAnswerNumber, answerTypeIcon);
        }
    }

    public void UpdateMemory(out bool hasForgotten)
    {
        float progressDelta = Time.deltaTime / _data.MemoryDuration;
        _memoryProgress = Mathf.Clamp01(_memoryProgress - progressDelta);
        hasForgotten = _memoryProgress <= 0;
        _playerView.MemoryUI.SetPercent(_memoryProgress);
        if (hasForgotten)
        {
            _playerView.MemoryUI.Hide();
        }
    }

    public bool TryGetRememberedAnswer(out int answerNumber)
    {
        if (IsRemembering)
        {
            answerNumber = _rememberedAnswerNumber;
            return true;
        }
        answerNumber = 0;
        return false;
    }
}
