using PrimeTween;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum EInteraction
{
    Undefined,
    PickUp,
    Static,
}

public interface IInteractionOwner
{
    InteractionController InteractionController { get; }
}

public class InteractionController : MonoBehaviour
{
    [Serializable]
    public class Data
    {
        public EInteraction Type = EInteraction.Undefined;
        public int BaseScore = 100;
        public int EmptyHandsExtraScore = 50;
        public int CarryingExtraScore = -25;
    }

    [Serializable]
    public class BestInteractionViewData
    {
        [Header("Outline")]
        public bool UseOutline;
        public Outline Outline;
        [Header("Tween")]
        public bool UseScaleTween;
        public Transform TweenTarget;
        public TweenSettings<Vector3> StartTweenSettings;
        public TweenSettings<Vector3> StopTweenSettings;
    }

    [SerializeField] private Collider[] _triggers;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Data _data;
    [SerializeField] private BestInteractionViewData _bestInteractionViewData;
    [SerializeField] private GameObject _destroyVFX;
    [Tag]
    [SerializeField] private string _interactableTag = "Interaction";
    [Tag]
    [SerializeField] private string _notInteractableTag = "Untagged";
    [SerializeField] private bool _isEnabledByDefault = true;

    private int _bestInteractionCount;
    private Tween _scaleTween;
    private List<string> _whiteListedActorIDs = new(); // [AKP] If empty, all actors can interact with this.
    public bool IsEnabled { get; private set; }

    public EInteraction Type => _data.Type;
    public int BaseScore => _data.BaseScore;
    public int EmptyHandsExtraScore => _data.EmptyHandsExtraScore;
    public int CarryingExtraScore => _data.CarryingExtraScore;
    public Vector3 Position => transform.position;
    public Rigidbody Rigidbody => _rigidbody;

    public event Action<InteractionController> OnDisableEvent;
    public event Action<InteractionController> OnDestroyEvent;

    private void Awake()
    {
        if (_destroyVFX != null)
        {
            _destroyVFX.gameObject.SetActive(false);
        }
        if (_bestInteractionViewData.UseOutline)
        {
            _bestInteractionViewData.Outline.enabled = false;
        }
        if (_bestInteractionViewData.UseScaleTween)
        {
            _bestInteractionViewData.StartTweenSettings.startFromCurrent = true;
            _bestInteractionViewData.StopTweenSettings.startFromCurrent = true;
        }

        SetIsEnabled(_isEnabledByDefault);
    }

    public void IncreaseBestInteractionCount()
    {
        _bestInteractionCount++;
        if (_bestInteractionCount == 1)
        {
            TriggerBestInteractionTween(isBestInteraction: true);
        }
    }

    public void DecreaseBestInteractionCount()
    {
        _bestInteractionCount--;
        if (_bestInteractionCount == 0)
        {
            TriggerBestInteractionTween(isBestInteraction: false);
        }
    }

    public void OnStartInteraction()
    {
        if (_data.Type == EInteraction.PickUp)
        {
            _rigidbody.isKinematic = true;
            Disable();
        }
    }

    public void OnStopInteraction()
    {
        if (_data.Type == EInteraction.PickUp)
        {
            _rigidbody.isKinematic = false;
            Enable();
        }
    }

    public void AddPlayerToWhitelist(string actorID)
    {
        _whiteListedActorIDs.Add(actorID);
    }

    public bool CanInteract(string actorID)
    {
        return _whiteListedActorIDs.Count == 0 || _whiteListedActorIDs.Contains(actorID);
    }

    public void Enable()
    {
        SetIsEnabled(true);
    }

    public void Disable()
    {
        SetIsEnabled(false);
        TriggerBestInteractionTween(isBestInteraction: false);
        OnDisableEvent?.Invoke(this);
    }

    private void SetIsEnabled(bool value)
    {
        IsEnabled = value;
        string colliderTag = value ? _interactableTag : _notInteractableTag;
        foreach (var trigger in _triggers)
        {
            if (trigger.isTrigger)
            {
                trigger.enabled = value;
            }
            else // isCollider
            {
                trigger.tag = colliderTag;
            }
        }
    }

    private void TriggerBestInteractionTween(bool isBestInteraction)
    {
        if (_bestInteractionViewData.UseOutline)
        {
            _bestInteractionViewData.Outline.enabled = isBestInteraction;
        }
        if (_bestInteractionViewData.UseScaleTween)
        {
            _scaleTween.Stop();
            if (isBestInteraction)
            {
                _scaleTween = Tween.Scale(_bestInteractionViewData.TweenTarget, _bestInteractionViewData.StartTweenSettings);
            }
            else if (_bestInteractionViewData.TweenTarget.localScale != _bestInteractionViewData.StopTweenSettings.endValue)
            {
                _scaleTween = Tween.Scale(_bestInteractionViewData.TweenTarget, _bestInteractionViewData.StopTweenSettings);
            }
        }
    }

    private void OnDestroy()
    {
        if (_destroyVFX != null)
        {
            _destroyVFX.transform.SetParent(null, worldPositionStays: true);
            _destroyVFX.gameObject.SetActive(true);
        }
        OnDestroyEvent?.Invoke(this);
    }
}
