using System.Collections.Generic;

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
        bool canPickUp = !_activeInteractions.Exists(x => x.Type == EInteraction.PickUp);

        InteractionController bestInteraction = null;
        foreach (InteractionController interaction in _interactions)
        {
            if (!canPickUp && interaction.Type == EInteraction.PickUp) continue;

            bestInteraction = interaction; // TODO: Logic
        }

        if (BestInteraction != bestInteraction)
        {
            BestInteraction?.DecreaseBestInteractionCount();
            BestInteraction = bestInteraction;
            bestInteraction?.IncreaseBestInteractionCount();
        }
    }

    public bool TryGetPickedUpInteraction(out InteractionController interaction)
    {
        interaction = _activeInteractions.Find(x => x.Type == EInteraction.PickUp);
        return interaction != null;
    }

    public bool TryStartInteraction(out InteractionController startedInteraction)
    {
        startedInteraction = BestInteraction;
        if (startedInteraction == null)
        {
            return false;
        }

        _activeInteractions.Add(startedInteraction);
        startedInteraction.OnStartInteraction();
        return true;
    }

    public bool TryStopInteraction(InteractionController stoppedInteraction)
    {
        if (!_activeInteractions.Remove(stoppedInteraction)) return false;
        stoppedInteraction.OnStopInteraction();
        return true;
    }
}
