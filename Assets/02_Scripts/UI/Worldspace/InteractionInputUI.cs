using System;
using UnityEngine;

public class InteractionInputUI : MonoBehaviour
{
    [SerializeField] private InteractionController _interaction;
    [SerializeField] private InputWorldspaceUI _uiPrefab;
    [SerializeField] private Transform _uiParentOverride;
    [SerializeField] private EAction _action;
    [SerializeField] private InputIconConfig _config;

    private InputWorldspaceUI _ui;
    private PlayerInputHandler _inputHandler;
    private bool _isSetup;

    public void Setup(PlayerInputHandler inputHandler)
    {
        if (_isSetup) return;
        _isSetup = true;
        _inputHandler = inputHandler;
        _ui = Instantiate(_uiPrefab);

        Transform target = _uiParentOverride != null ? _uiParentOverride : _interaction.transform;
        _ui.SetTarget(target);
        _interaction.OnBestInteractionStart += OnBestInteractionStart;
        _interaction.OnBestInteractionStop += OnBestInteractionStop;
        _inputHandler.DeviceChangedEvent += OnDeviceChanged;
        if (_interaction.IsBestInteraction)
            OnBestInteractionStart();
    }

    private void OnDestroy()
    {
        if (_interaction != null)
        {
            _interaction.OnBestInteractionStart -= OnBestInteractionStart;
            _interaction.OnBestInteractionStop -= OnBestInteractionStop;
        }
        if (_inputHandler != null)
            _inputHandler.DeviceChangedEvent -= OnDeviceChanged;
        if (_ui != null)
            Destroy(_ui.gameObject);
    }

    private void OnBestInteractionStart()
    {
        if (_inputHandler == null) return;
        ShowForDevice(_inputHandler.LastKnownDeviceType);
    }

    private void OnBestInteractionStop()
    {
        _ui.Hide();
    }

    private void OnDeviceChanged(EDevice device)
    {
        if (_ui.gameObject.activeSelf)
            ShowForDevice(device);
    }

    private void ShowForDevice(EDevice device)
    {
        Sprite sprite = _config.GetSprite(_action, device);
        if (sprite != null)
            _ui.Show(sprite);
    }
}
