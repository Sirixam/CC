using UnityEngine;
using UnityEngine.Events;

public class TutorialManager : MonoBehaviour
{
    public enum EShowBehavior
    {
        Always,
        OncePerSession,
        Once,
    }

    [SerializeField] private string _tutorialID;
    [SerializeField] private EShowBehavior _showBehavior;
    [SerializeField] private UnityEvent _onShow;

    public bool BlockGameStart => GetComponent<TutorialInputAdvancer>().BlockGameStart;

    private void Start()
    {
        if (ShouldShow())
            Show();
    }

    private bool ShouldShow()
    {
        return _showBehavior switch
        {
            EShowBehavior.Always => true,
            EShowBehavior.OncePerSession => !TutorialSession.WasShown(_tutorialID),
            EShowBehavior.Once => true, // save check not yet implemented; always shows
            _ => false,
        };
    }

    private void Show()
    {
        switch (_showBehavior)
        {
            case EShowBehavior.OncePerSession:
                TutorialSession.MarkShown(_tutorialID);
                break;
            case EShowBehavior.Once:
                Debug.Log("SAVE SYSTEM NOT IMPLEMENTED");
                break;
        }

        _onShow.Invoke();
    }
}
