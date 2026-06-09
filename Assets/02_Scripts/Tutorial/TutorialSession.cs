using System.Collections.Generic;

public static class TutorialSession
{
    private static readonly Dictionary<string, int> _shownCounts = new();

    public static bool WasShown(string tutorialID) => GetShownCount(tutorialID) > 0;
    public static void MarkShown(string tutorialID) => IncrementShownCount(tutorialID);

    public static int GetShownCount(string tutorialID) =>
        _shownCounts.TryGetValue(tutorialID, out int count) ? count : 0;

    public static void IncrementShownCount(string tutorialID)
    {
        _shownCounts.TryGetValue(tutorialID, out int count);
        _shownCounts[tutorialID] = count + 1;
    }
}
