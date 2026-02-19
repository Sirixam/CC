using UnityEngine;

public static class GameContext
{
    public static ItemsManager ItemsManager { get; private set; }
    public static AnswersManager AnswersManager { get; private set; }

    public static bool HasAnswersManager => AnswersManager != null;
    public static bool HasItemsManager => ItemsManager != null;

    public static void Initialize(ItemsManager itemsManager, AnswersManager answersManager)
    {
        ItemsManager = itemsManager;
        AnswersManager = answersManager;

        // Validations
        if (ItemsManager == null)
        {
            Debug.LogError("GameContext initialized without a valid ItemsManager reference.");
        }
        if (AnswersManager == null)
        {
            Debug.LogError("GameContext initialized without a valid AnswersManager reference.");
        }
    }
}

