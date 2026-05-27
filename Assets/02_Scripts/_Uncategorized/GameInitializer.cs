using UnityEngine;

[DefaultExecutionOrder(-1000)] // Ensure this runs before other scripts
public class GameInitializer : MonoBehaviour
{
    [Header("Core Managers")]
    [SerializeField] private ItemsManager _itemsManager;
    [SerializeField] private AnswersManager _answersManager;
    [SerializeField] private StudentManager _studentManager;

    private void Awake()
    {
        GameContext.Initialize(_itemsManager, _answersManager, _studentManager);
    }
}

