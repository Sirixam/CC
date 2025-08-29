using System;
using UnityEngine;
using UnityEngine.UI;

public class TestInputScopes : MonoBehaviour
{
    [Serializable]
    public class InputScopeData
    {
        public EInputScope ScopeType;
        public Toggle Toggle;
    }

    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private InputScopeData[] _inputScopeData;
    [SerializeField] private EInputScope _initialScopeType;

    private void Awake()
    {
        foreach (var inputScopeData in _inputScopeData)
        {
            inputScopeData.Toggle.isOn = _inputHandler.ScopeType == inputScopeData.ScopeType;
            inputScopeData.Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    _inputHandler.SetScope(inputScopeData.ScopeType);
                }
            });
        }
    }

    private void Start()
    {
        InputScopeData initialInputScopeData = Array.Find(_inputScopeData, x => x.ScopeType == _initialScopeType);
        if (initialInputScopeData != null)
        {
            initialInputScopeData.Toggle.isOn = true;
        }
    }
}
