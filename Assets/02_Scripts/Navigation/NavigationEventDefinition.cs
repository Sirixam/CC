using UnityEngine;

[CreateAssetMenu(fileName = "DEF_NavigationEvent", menuName = "Definitions/Navigation Event")]
public class NavigationEventDefinition : ScriptableObject
{
    public void Execute(IActor actor)
    {
        Debug.LogError("Execute Navigation Event: " + name);
    }
}
