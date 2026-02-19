using UnityEngine;

[DefaultExecutionOrder(-1000)] // Ensure this runs before other scripts
public class GameInitializer : MonoBehaviour
{
    [Header("Core Managers")]
    [SerializeField] private ItemsManager _itemsManager;
    [SerializeField] private AnswersManager _answersManager;

    private void Awake()
    {
        GameContext.Initialize(_itemsManager, _answersManager);
    }
}

