using PrimeTween;
using System;
using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private Transform _bestInteractionTweenTarget;
    [SerializeField] private TweenSettings<Vector3> _startBestInteractionTween;
    [SerializeField] private TweenSettings<Vector3> _stopBestInteractionTween;
    [SerializeField] private bool _disableOnRequest;

    private int _bestInteractionCount;
    private Tween _tween;

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

    public void OnRequest()
    {
        if (!_disableOnRequest) return;
        Disable();
    }

    private void Disable()
    {
        gameObject.SetActive(false);
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
