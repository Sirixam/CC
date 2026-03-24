using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;


public class TestPageView : MonoBehaviour
{
    //had to use quaternions instead of euler angles, since the tween animation didn't use the shortest path to do the lifting of the page
    [SerializeField] private RectTransform _paper;

    [Header("Lifted State")]
    [SerializeField] private RectTransform _liftedTarget;
    [SerializeField] private float _tweenDuration = 0.3f;
    private Sequence _liftPaperSequence;

    private Vector3 _restingLocalPosition;
    private Vector3 _liftedLocalPosition;  // no more [SerializeField]

    private Quaternion _restingRotation;       // for rotation
    private Quaternion _liftedRotation;

    public void Awake()
    {
        _liftedLocalPosition = _liftedTarget.localPosition;
        _liftedRotation = _liftedTarget.localRotation;
        _liftedTarget.gameObject.SetActive(false);
        _restingLocalPosition = _paper.localPosition;
        _restingRotation = _paper.localRotation;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"Current localPosition: {_paper.localPosition}");
            Debug.Log($"Current localEulerAngles: {_paper.localEulerAngles}");
        }
    }
    public void Lift()
    {
        _liftPaperSequence.Stop();
        _liftPaperSequence = Sequence.Create()
            .Group(Tween.LocalPosition(_paper, _liftedLocalPosition, _tweenDuration, Ease.OutBack))
            .Group(Tween.LocalRotation(_paper, _liftedRotation, _tweenDuration, Ease.OutBack)
        );
    }

    public void Lower()
    {
        _liftPaperSequence.Stop();
        _liftPaperSequence = Sequence.Create()
            .Group(Tween.LocalPosition(_paper, _restingLocalPosition, _tweenDuration, Ease.OutBack))
            .Group(Tween.LocalRotation(_paper, _restingRotation, _tweenDuration, Ease.OutBack)
        );
    }

}