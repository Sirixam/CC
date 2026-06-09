using System;
using UnityEngine;
using UnityEngine.UI;

public class ButtonListener : MonoBehaviour
{
    [SerializeField] private Button _button;

    public Action OnClickEvent;

    private void Awake()
    {
        _button.onClick.AddListener(BtnListener);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(BtnListener);
    }

    public void BtnListener()
    {
        OnClickEvent?.Invoke();
    }
}
