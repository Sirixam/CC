using PrimeTween;
using System;
using UnityEngine;

public enum EInteraction
{
    Undefined,
    PickUp,
    Static,
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

    [SerializeField] private SphereCollider _trigger;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Data _data;
    [SerializeField] private BestInteractionViewData _bestInteractionViewData;

    private int _bestInteractionCount;
    private Tween _scaleTween;

    public EInteraction Type => _data.Type;
    public int BaseScore => _data.BaseScore;
    public int EmptyHandsExtraScore => _data.EmptyHandsExtraScore;
    public int CarryingExtraScore => _data.CarryingExtraScore;
    public Vector3 Position => transform.position;

    public Rigidbody Rigidbody => _rigidbody;

    public event Action<InteractionController> OnDisableEvent;

    private void Awake()
    {
        if (_bestInteractionViewData.UseOutline)
        {
            _bestInteractionViewData.Outline.enabled = false;
        }
        if (_bestInteractionViewData.UseScaleTween)
        {
            _bestInteractionViewData.StartTweenSettings.startFromCurrent = true;
            _bestInteractionViewData.StopTweenSettings.startFromCurrent = true;
        }
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

    private void Enable()
    {
        _trigger.enabled = true;
    }

    private void Disable()
    {
        _trigger.enabled = false;
        TriggerBestInteractionTween(isBestInteraction: false);
        OnDisableEvent?.Invoke(this);
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
}
