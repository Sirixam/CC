using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using System;

public class LivesUI : MonoBehaviour
{
    [SerializeField] private Image[] _lives;
    [SerializeField] private Color _defaultColor = Color.white;
    [SerializeField] private Color _emptyColor = Color.black;
    [SerializeField] private Sprite _lostLife;

    [Header("Peel Settings")]
    [SerializeField] private float _peelDuration = 0.6f;
    [SerializeField] private float _fallDuration = 0.4f;
    [SerializeField] private float _fallDistance = 600f;
    [SerializeField] private float _peelAngle = 120f;

    private Vector2 _originalPivot;
    private Vector3 _originalPosition;

    private Sequence _shrinkTween;

    // Diagonal fold axis: perpendicular to peel direction (top-right → bottom-left)
    private static readonly Vector3 FoldAxis = new Vector3(1f, -1f, 0f).normalized;

    public void SetLives(int value)
    {
        for (int i = 0; i < _lives.Length; i++)
        {
            if (i < value)
            {
                _lives[i].gameObject.SetActive(true);
                _lives[i].color = _defaultColor;
            }
        }
    }

    public void playLostLifeAnimation(int lifeIndex, Action onComplete)
    {
        RectTransform _target = _lives[lifeIndex].rectTransform;

        _originalPivot = _target.pivot;

        // Pivot at top-right — that's the corner being pulled
        SetPivotWithoutMoving(_target, new Vector2(0f, 0f));

        _target.localScale = Vector3.one;
        _target.localRotation = Quaternion.identity;
        //_canvasGroup.alpha = 1f;

        // Peel rotation: rotate around diagonal axis
        Quaternion peelTarget = Quaternion.AngleAxis(_peelAngle, FoldAxis);

        Sequence.Create()
            // Phase 1: Peel — diagonal curl from top-right corner
            .Chain(Tween.Rotation(_target, peelTarget, _peelDuration, Ease.InSine))

            // Phase 2: Fall — drop away, fade out
            .Chain(Tween.LocalPositionY(_target, _target.localPosition.y - _fallDistance, _fallDuration, Ease.InQuad))
            .Group(Tween.LocalPositionX(_target, _target.localPosition.x - _fallDistance * 0.3f, _fallDuration, Ease.InQuad))
            //.Group(Tween.Alpha(_canvasGroup, 0f, _fallDuration * 0.6f, Ease.InQuad))
            .Group(Tween.Scale(_target, Vector3.one * 0.6f, _fallDuration, Ease.InQuad))

            .OnComplete(() =>
            {
                SetPivotWithoutMoving(_target, _originalPivot);
                onComplete?.Invoke();
            });
    }
    private void SetPivotWithoutMoving(RectTransform rect, Vector2 newPivot)
    {
        Vector2 size = rect.rect.size;
        Vector2 deltaPivot = newPivot - rect.pivot;
        Vector3 deltaPosition = new Vector3(
            deltaPivot.x * size.x * rect.localScale.x,
            deltaPivot.y * size.y * rect.localScale.y,
            0f
        );
        rect.pivot = newPivot;
        rect.localPosition += deltaPosition;
    }
}
