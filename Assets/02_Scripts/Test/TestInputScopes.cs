using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class TestInputScopes : MonoBehaviour
{
    [Serializable]
    public class InputScopeData
    {
        public EInputScope ScopeType;
        public Toggle Toggle;
    }

    [Serializable]
    public class PlayerData
    {
        public GameObject Container;
        public TMP_Text ControlScheme;
        public TMP_Text Feedback;
        public InputScopeData[] InputScopeData;
        public Color Color;

        public PlayerInputHandler InputHandler { get; set; }
    }

    [SerializeField] private int _playersCount;
    [SerializeField] private PlayerData[] _playersInputData;
    [SerializeField] private Camera _camera;
    [SerializeField] private EInputScope _initialScopeType;

    private void Awake()
    {
        foreach (var playerData in _playersInputData)
        {
            foreach (var inputScopeData in playerData.InputScopeData)
            {
                inputScopeData.Toggle.isOn = false;
                inputScopeData.Toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        playerData.InputHandler.SetScope(inputScopeData.ScopeType);
                    }
                });
            }

            playerData.Container.SetActive(false);
        }
    }

    public void PlayerJoined(PlayerInput playerInput)
    {
       
        //Prevent overflow
        if (_playersCount >= _playersInputData.Length)
        {
            Debug.LogWarning("More PlayerInputs joined than PlayerData slots.");
            return;
        }

        PlayerInputHandler inputHandler =
            playerInput.GetComponent<PlayerInputHandler>();
        PlayerData playerData = _playersInputData[_playersCount++];
        playerInput.camera = _camera;

        // Initialize Input Handler
        inputHandler.Initialize();
        inputHandler.ActionEvent += actionType => playerData.Feedback.text = $"{actionType} requested";
        inputHandler.DirectionalActionEvent += (actionType, input) => playerData.Feedback.text = $"{actionType} requested with input: {input}";
        inputHandler.HoldActionEvent += (actionType, isHolding) => playerData.Feedback.text = isHolding ? $"{actionType} hold begin" : $"{actionType} hold end";
        playerData.InputHandler = inputHandler;

        // Set initial scope type
        InputScopeData initialInputScopeData = Array.Find(playerData.InputScopeData, x => x.ScopeType == _initialScopeType);
        if (initialInputScopeData != null)
        {
            initialInputScopeData.Toggle.isOn = true;
        }

        // Show initial feedback
        playerData.ControlScheme.text = $"Control scheme: {playerInput.currentControlScheme}";
        playerData.Container.SetActive(true);

        // Initialize Color (Optional)
        var colorCompoonent = playerInput.GetComponentInChildren<ColorComponent>();
        colorCompoonent?.SetColor(playerData.Color);
    }
}
