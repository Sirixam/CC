using System.Collections.Generic;

public class InteractionHelper
{
    private List<InteractionController> _interactions = new();

    public InteractionController BestInteraction { get; private set; }

    public void AddInteraction(InteractionController interactionController)
    {
        _interactions.Add(interactionController);
        UpdateBestInteraction();
    }

    public void RemoveInteraction(InteractionController interactionController)
    {
        _interactions.Remove(interactionController);
        UpdateBestInteraction();
    }

    public void UpdateBestInteraction()
    {
        BestInteraction = _interactions.Count > 0 ? _interactions[0] : null; // TODO
    }
}
