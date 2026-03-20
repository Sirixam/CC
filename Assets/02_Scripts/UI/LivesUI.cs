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
    private Sequence _shrinkTween;

    public void SetLives(int value)
    {
        for (int i = 0; i < _lives.Length; i++)
        {
            if (i < value)
            {
                _lives[i].color = _defaultColor;
            }
            else
            {
                _lives[i].color = _emptyColor;
            }
        }
    }

     public void playLostLifeAnimation(int lifeIndex, Action onComplete)
    {
        RectTransform target = _lives[lifeIndex].rectTransform;

        _shrinkTween.Stop();
        _shrinkTween = Sequence.Create()
            .Chain(Tween.Scale(target, new Vector3(1f, 1f, 1f), 0.5f, Ease.OutQuad))
            .Chain(Tween.Scale(target, Vector3.zero, 1f, Ease.InBack)
            .OnComplete(() =>
            {
                target.localScale = Vector3.one; // reset for next game
                onComplete?.Invoke();
            }));
    }
        
}
