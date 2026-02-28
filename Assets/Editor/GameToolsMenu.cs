using UnityEditor;

public static class GameToolsMenu
{
    public const string UseTestDefinitionKey = "GameTools.UseTestDefinition";
    private const string MenuPath = "Game Tools/Enable Test Mode";

    [MenuItem(MenuPath)]
    private static void ToggleUseTest()
    {
        bool newValue = !EditorPrefs.GetBool(UseTestDefinitionKey, false);
        EditorPrefs.SetBool(UseTestDefinitionKey, newValue);
        Menu.SetChecked(MenuPath, newValue);
    }

    [MenuItem(MenuPath, validate = true)]
    private static bool ValidateToggleUseTest()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(UseTestDefinitionKey, false));
        return true;
    }
}
