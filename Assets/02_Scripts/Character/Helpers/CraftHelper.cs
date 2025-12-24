using UnityEngine;

public class CraftHelper
{
    private PlayerView _actorView;
    private InteractionHelper _interactionHelper;
    private ItemsManager _itemsManager;

    private Vector3 PickUpPosition => _actorView.PickUpPosition + Vector3.up; // Slightly above to highlight briefly.

    public CraftHelper(PlayerView actorView, InteractionHelper interactionHelper)
    {
        _actorView = actorView;
        _interactionHelper = interactionHelper;
        _itemsManager = ItemsManager.GetInstance();
    }

    public void CraftItem(string itemName)
    {
        GameObject itemInstance = _itemsManager.InstantiateItem(itemName, PickUpPosition, Quaternion.identity, parent: null);
        _actorView.OnPickUp(itemInstance.transform);
        if (itemInstance.TryGetComponent(out IInteractionOwner interactionOwner))
        {
            _interactionHelper.AddInteraction(interactionOwner.InteractionController);
            _interactionHelper.StartInteraction(interactionOwner.InteractionController);
        }
    }

    public void CraftAnswer(string answerID)
    {
        PaperBallController answerInstance = _itemsManager.InstantiateAnswer(PickUpPosition, Quaternion.identity, parent: null);
        answerInstance.SetAnswer(answerID);
        _actorView.OnPickUp(answerInstance.transform);
        _interactionHelper.AddInteraction(answerInstance.InteractionController);
        _interactionHelper.StartInteraction(answerInstance.InteractionController);
    }
}
