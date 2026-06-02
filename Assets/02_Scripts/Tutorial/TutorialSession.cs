using System.Collections.Generic;

public static class TutorialSession
{
    private static readonly HashSet<string> _shown = new();

    public static bool WasShown(string tutorialID) => _shown.Contains(tutorialID);
    public static void MarkShown(string tutorialID) => _shown.Add(tutorialID);
}
