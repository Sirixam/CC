using UnityEngine;

public class HelpUI : MonoBehaviour
{
    [SerializeField] private GameObject _gamepadContainer;
    [SerializeField] private GameObject _keyboardContainer;

    public void Show(EDevice deviceType)
    {
        gameObject.SetActive(true);
        switch (deviceType)
        {
            case EDevice.Gamepad: _gamepadContainer.SetActive(true); break;
            case EDevice.KeyboardAndMouse: _keyboardContainer.SetActive(true); break;
            default:
                Debug.LogError("Device type is not being handled: " + deviceType);
                break;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        _gamepadContainer.SetActive(false);
        _keyboardContainer.SetActive(false);
    }
}
