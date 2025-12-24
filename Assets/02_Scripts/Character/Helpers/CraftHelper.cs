using UnityEngine;

public class CraftHelper
{
    private PlayerView _actorView;
    private InteractionHelper _interactionHelper;
    private ItemsManager _itemsManager;

    public CraftHelper(PlayerView actorView, InteractionHelper interactionHelper)
    {
        _actorView = actorView;
        _interactionHelper = interactionHelper;
        _itemsManager = ItemsManager.GetInstance();
    }

    public void CraftAnswer(string answerID)
    {
        PaperBallController answerInstance = _itemsManager.InstantiateAnswer(_actorView.PickUpPosition + Vector3.up, Quaternion.identity, parent: null); // Slightly above to highlight briefly.
        answerInstance.SetAnswer(answerID);
        _actorView.OnPickUp(answerInstance.transform);
        _interactionHelper.AddInteraction(answerInstance.InteractionController);
        _interactionHelper.StartInteraction(answerInstance.InteractionController);
    }
}
