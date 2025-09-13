using PrimeTween;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private Transform _bestInteractionTweenTarget;
    [SerializeField] private TweenSettings<Vector3> _startBestInteractionTween;
    [SerializeField] private TweenSettings<Vector3> _stopBestInteractionTween;

    private bool _isBestInteraction;
    private List<InteractionHelper> _interactionHelpers = new();
    private Tween _tween;

    private void Awake()
    {
        _startBestInteractionTween.startFromCurrent = true;
        _stopBestInteractionTween.startFromCurrent = true;
    }

    private void Update()
    {
        bool isBestInteraction = _interactionHelpers.Exists(x => x.BestInteraction == this);
        if (isBestInteraction == _isBestInteraction) return;

        _tween.Stop();
        _isBestInteraction = isBestInteraction;
        if (isBestInteraction)
        {
            _tween = Tween.Scale(_bestInteractionTweenTarget, _startBestInteractionTween);
        }
        else
        {
            _tween = Tween.Scale(_bestInteractionTweenTarget, _stopBestInteractionTween);
        }
    }

    public void OnPlayerEnter(InteractionHelper interactionHelper)
    {
        _interactionHelpers.Add(interactionHelper);
        interactionHelper.AddInteraction(this);
    }

    public void OnPlayerExit(InteractionHelper interactionHelper)
    {
        _interactionHelpers.Remove(interactionHelper);
        interactionHelper.RemoveInteraction(this);
    }
}
