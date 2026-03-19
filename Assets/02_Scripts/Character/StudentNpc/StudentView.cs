using AKGaming.Game;
using UnityEngine;

public class StudentView : MonoBehaviour
{
    [SerializeField] private GameObject _handObject;
    [SerializeField] private HandWritingLoopController _handWritingLoopController;

    private void Awake()
    {
        _handWritingLoopController.enabled = false;
        _handObject.SetActive(false);
    }

    public void StartAnswering()
    {
        _handObject.SetActive(true);
        _handWritingLoopController.enabled = true;
    }

    public void StartValidating()
    {
        _handWritingLoopController.enabled = false;
        _handObject.SetActive(false);
    }
}
