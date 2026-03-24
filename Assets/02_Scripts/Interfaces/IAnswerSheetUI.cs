using UnityEngine;

public interface IAnswerSheetUI
{
    void Setup(Answer[] answers);
    void Show();
    void Hide();
    void SetAnswerState(string answerID, bool isFilled);
    void SetCorrectness(string answerID, float correctness);
    void HideProgress();
    void HideProgress(string answerID);
    void SetProgress(string answerID, float progress);
    void ShowProgress(string answerID, float progress);
    void ResetAnswerStates();
}
