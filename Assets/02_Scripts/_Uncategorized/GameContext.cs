using UnityEngine;

public static class GameContext
{
    public static ItemsManager ItemsManager { get; private set; }
    public static AnswersManager AnswersManager { get; private set; }
    public static StudentManager StudentManager { get; private set; }

    public static bool HasAnswersManager => AnswersManager != null;
    public static bool HasItemsManager => ItemsManager != null;

    public static void Initialize(ItemsManager itemsManager, AnswersManager answersManager, StudentManager studentManager)
    {
        ItemsManager = itemsManager;
        AnswersManager = answersManager;
        StudentManager = studentManager;

        // Validations
        if (ItemsManager == null)
            Debug.LogError("GameContext initialized without a valid ItemsManager reference.");
        if (AnswersManager == null)
            Debug.LogError("GameContext initialized without a valid AnswersManager reference.");
        if (StudentManager == null)
            Debug.LogError("GameContext initialized without a valid StudentManager reference.");
    }
}

