using UnityEngine;

public class StudentView : MonoBehaviour
{
    [SerializeField] private HandAnimator _handAnimator;

    public void StartThinking(TestPageView testPageView)
    {
        _handAnimator.SetHidden();
        testPageView?.Lower();
    }

    public void StartAnswering()
    {
        _handAnimator.SetWriting();
    }

    public void StartValidating(TestPageView testPageView)
    {
        _handAnimator.SetValidating();
        testPageView?.Lift();
    }
}
