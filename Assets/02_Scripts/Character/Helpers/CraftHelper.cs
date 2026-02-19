using UnityEngine;

public class CraftHelper
{
    public struct CraftData
    {
        public string ItemName;
        public float CraftDuration;
        public float RemainingTime;

        public float CraftPercent => 1 - RemainingTime / CraftDuration;
    }

    private PlayerView _actorView;
    private InteractionHelper _interactionHelper;

    private CraftData _craftData;
    private Vector3 PickUpPosition => _actorView.PickUpPosition + Vector3.up; // Slightly above to highlight briefly.

    public bool IsCrafting { get; private set; }

    public CraftHelper(PlayerView actorView, InteractionHelper interactionHelper)
    {
        _actorView = actorView;
        _interactionHelper = interactionHelper;
    }

    public void UpdateCrafting(float deltaTime)
    {
        _craftData.RemainingTime -= deltaTime;
        _actorView.CraftingUI.SetPercent(_craftData.CraftPercent);
        if (_craftData.RemainingTime <= 0)
        {
            IsCrafting = false;
            CraftItem(_craftData.ItemName);
            _actorView.CraftingUI.Hide();
        }
    }

    public bool TryStartCraftingItem(string itemName)
    {
        if (IsCrafting)
        {
            Debug.LogError($"Failed to start crafting item: {itemName}, already crafting another item: {_craftData.ItemName}");
            return false;
        }

        if (!GameContext.ItemsManager.TryGetCraftData(itemName, out float craftDuration))
        {
            Debug.LogError("Craft data was not found for item: " + itemName);
            return false;
        }

        IsCrafting = true;
        _craftData = new CraftData { ItemName = itemName, CraftDuration = craftDuration, RemainingTime = craftDuration };
        _actorView.CraftingUI.Show();
        _actorView.CraftingUI.SetPercent(_craftData.CraftPercent);
        return true;
    }

    public bool TryStopCraftingItem()
    {
        if (!IsCrafting) return false;

        IsCrafting = false;
        _actorView.CraftingUI.Hide();
        return true;
    }

    public void CraftItem(string itemName)
    {
        GameObject itemInstance = GameContext.ItemsManager.InstantiateItem(itemName, PickUpPosition, Quaternion.identity, parent: null);
        _actorView.OnPickUp(itemInstance.transform);
        if (itemInstance.TryGetComponent(out IInteractionOwner interactionOwner))
        {
            _interactionHelper.AddInteraction(interactionOwner.InteractionController);
            _interactionHelper.StartInteraction(interactionOwner.InteractionController);
        }
    }

    public void CraftAnswer(string answerID, float correctness)
    {
        PaperBallController answerInstance = GameContext.ItemsManager.InstantiateAnswer(PickUpPosition, Quaternion.identity, parent: null);
        answerInstance.SetAnswer(answerID, correctness);
        _actorView.OnPickUp(answerInstance.transform);
        _interactionHelper.AddInteraction(answerInstance.InteractionController);
        _interactionHelper.StartInteraction(answerInstance.InteractionController);
    }
}
