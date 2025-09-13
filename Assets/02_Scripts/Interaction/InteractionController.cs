using PrimeTween;
using System;
using UnityEngine;

public enum EInteraction
{
    Undefined,
    PickUp,
}

public class InteractionController : MonoBehaviour
{
    [SerializeField] private EInteraction _type;
    [SerializeField] private GameObject _trigger;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Transform _bestInteractionTweenTarget;
    [SerializeField] private TweenSettings<Vector3> _startBestInteractionTween;
    [SerializeField] private TweenSettings<Vector3> _stopBestInteractionTween;

    private int _bestInteractionCount;
    private Tween _tween;

    public EInteraction Type => _type;

    public event Action<InteractionController> OnDisableEvent;

    private void Awake()
    {
        _startBestInteractionTween.startFromCurrent = true;
        _stopBestInteractionTween.startFromCurrent = true;
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
        if (_type == EInteraction.PickUp)
        {
            _rigidbody.isKinematic = true;
            Disable();
        }
    }

    public void OnStopInteraction()
    {
        if (_type == EInteraction.PickUp)
        {
            _rigidbody.isKinematic = false;
            Enable();
        }
    }

    private void Enable()
    {
        _trigger.SetActive(true);
    }

    private void Disable()
    {
        _trigger.SetActive(false);
        TriggerBestInteractionTween(isBestInteraction: false);
        OnDisableEvent?.Invoke(this);
    }

    private void TriggerBestInteractionTween(bool isBestInteraction)
    {
        _tween.Stop();
        if (isBestInteraction)
        {
            _tween = Tween.Scale(_bestInteractionTweenTarget, _startBestInteractionTween);
        }
        else
        {
            _tween = Tween.Scale(_bestInteractionTweenTarget, _stopBestInteractionTween);
        }
    }
}
