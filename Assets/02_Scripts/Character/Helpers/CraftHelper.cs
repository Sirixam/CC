using System;
using UnityEngine;

public interface ICraftService
{
    bool TryGetCraftData(string itemName, out float craftDuration);
    GameObject InstantiateItem(string itemName, Vector3 position, Quaternion rotation, Transform parent);
    PaperBallController InstantiateAnswer(Vector3 position, Quaternion rotation, Transform parent);
}

public class CraftHelper
{
    public struct CraftData
    {
        public string ItemName;
        public float CraftDuration;
        public float RemainingTime;

        public float CraftPercent => 1 - RemainingTime / CraftDuration;
    }

    private IActor _actor;
    private PlayerView _actorView;
    private InteractionHelper _interactionHelper;
    private ICraftService _craftService;
    private CraftData _craftData;
    private bool _shouldDropBeforeCraft;
    private Vector3 PickUpPosition => _actorView.PickUpPosition + Vector3.up; // Slightly above to highlight briefly.

    public event Action OnFinishedCrafting;

    public bool IsCrafting { get; private set; }

    public CraftHelper(IActor actor, PlayerView actorView, InteractionHelper interactionHelper, ICraftService craftService)
    {
        _actor = actor;
        _actorView = actorView;
        _interactionHelper = interactionHelper;
        _craftService = craftService;
    }

    public void UpdateCrafting(float deltaTime)
    {
        _craftData.RemainingTime -= deltaTime;
        _actorView.CraftingUI.SetPercent(_craftData.CraftPercent);
        if (_craftData.RemainingTime <= 0)
        {
            IsCrafting = false;

            // Drop current item right before crafting new one
            if (_shouldDropBeforeCraft)
            {
                _actorView.StopShakeHeldItem();
                DropCurrentItem();
                _shouldDropBeforeCraft = false;
            }

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

        if (!_craftService.TryGetCraftData(itemName, out float craftDuration))
        {
            Debug.LogError("Craft data was not found for item: " + itemName);
            return false;
        }

        // Check if holding an item — start shaking it
        _shouldDropBeforeCraft = _interactionHelper.TryGetPickedUpInteraction(out _);
        if (_shouldDropBeforeCraft)
        {
            _actorView.ShakeHeldItem();
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

        // Stop shaking if we were going to drop
        if (_shouldDropBeforeCraft)
        {
            _actorView.StopShakeHeldItem();
            _shouldDropBeforeCraft = false;
        }

        _actorView.CraftingUI.Hide();
        return true;
    }

    public void CraftItem(string itemName)
    {
        GameObject itemInstance = _craftService.InstantiateItem(itemName, PickUpPosition, Quaternion.identity, parent: null);
        _actorView.OnPickUp(itemInstance.transform);
        if (itemInstance.TryGetComponent(out IInteractionOwner interactionOwner))
        {
            _interactionHelper.AddInteraction(interactionOwner.InteractionController);
            _interactionHelper.StartInteraction(interactionOwner.InteractionController);
        }
        if (itemInstance.TryGetComponent(out IPickUpInteractionOwner pickUpInteraction))
        {
            pickUpInteraction.OnPickedUp(_actor.ID);
        }
        OnFinishedCrafting?.Invoke();
    }

    public PaperBallController CraftAnswer(string answerID, float correctness, string contributorActorID)
    {
        PaperBallController answerInstance = _craftService.InstantiateAnswer(PickUpPosition, Quaternion.identity, parent: null);
        answerInstance.SetAnswer(answerID, correctness, contributorActorID);
        _actorView.OnPickUp(answerInstance.transform);
        _interactionHelper.AddInteraction(answerInstance.InteractionController);
        _interactionHelper.StartInteraction(answerInstance.InteractionController);
        return answerInstance;
    }

    private void DropCurrentItem()
    {
        if (_interactionHelper.TryGetPickedUpInteraction(out InteractionController stoppedInteraction))
        {
            _interactionHelper.TryStopInteraction(stoppedInteraction);
            _actorView.OnDrop(stoppedInteraction.transform);
            _actorView.HideThrowPreview();
            if (stoppedInteraction.TryGetComponent(out IPickUpInteractionOwner interactionOwner))
            {
                interactionOwner.OnDropped();
            }

            Vector3 sideDirection = UnityEngine.Random.value > 0.5f
                        ? _actorView.transform.right
                        : -_actorView.transform.right;

            // Add slight upward arc and forward push
            Vector3 throwForce = sideDirection * 1f + Vector3.up * 1.5f + _actorView.transform.forward * -0.5f;
            stoppedInteraction.Rigidbody.AddForce(throwForce, ForceMode.VelocityChange);
        }
    }
}
