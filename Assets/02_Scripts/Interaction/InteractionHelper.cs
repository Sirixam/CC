using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

public class InteractionHelper
{
    private List<InteractionController> _interactions = new();
    private List<InteractionController> _activeInteractions = new();

    public InteractionController BestInteraction { get; private set; }

    public void AddInteraction(InteractionController interaction)
    {
        interaction.OnDisableEvent += RemoveInteraction;
        _interactions.Add(interaction);
        UpdateBestInteraction();
    }

    public void RemoveInteraction(InteractionController interaction)
    {
        interaction.OnDisableEvent -= RemoveInteraction;
        _interactions.Remove(interaction);
        UpdateBestInteraction();
    }

    public void UpdateBestInteraction()
    {
        InteractionController bestInteraction = _interactions.Count > 0 ? _interactions[0] : null; // TODO
        if (BestInteraction != bestInteraction)
        {
            BestInteraction?.DecreaseBestInteractionCount();
            BestInteraction = bestInteraction;
            bestInteraction?.IncreaseBestInteractionCount();
        }
    }

    public bool TryInteract(out InteractionController interactionController)
    {
        interactionController = BestInteraction;
        if (interactionController == null)
        {
            return false;
        }

        _activeInteractions.Add(interactionController);
        interactionController.OnRequest();
        return true;
    }
}
