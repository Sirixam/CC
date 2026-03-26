using UnityEngine;

public class HandPinchController : MonoBehaviour
{
    [SerializeField] private HandProceduralAnimator _hand;

    [Header("Timing")]
    [SerializeField] private float _speed = 10f;

    [Header("Pinch Pose")]
    [Range(0f, 1f)][SerializeField] private float _thumbCurl = 0.6f;
    [Range(0f, 1f)][SerializeField] private float _thumbOppose = 1f;

    [Range(0f, 1f)][SerializeField] private float _indexCurl = 0.9f;
    [Range(0f, 1f)][SerializeField] private float _middleCurl = 0.5f;
    [Range(0f, 1f)][SerializeField] private float _ringCurl = 0.25f;
    [Range(0f, 1f)][SerializeField] private float _littleCurl = 0.15f;

    [Header("Relaxed Base")]
    [SerializeField] private float _relaxedCurl = 0.15f;

    [SerializeField] private bool _instantPinchOnStart;

    private float _target;   // 0 = open, 1 = pinch
    private float _current;

    private void Start()
    {
        if (_instantPinchOnStart)
        {
            _current = _target = 1f;
        }
    }

    private void Update()
    {
        _current = Mathf.MoveTowards(_current, _target, _speed * Time.deltaTime);
        ApplyPinch(_current);
    }

    private void ApplyPinch(float t)
    {
        // Blend from relaxed -> pinch pose
        float thumbCurl = Mathf.Lerp(_relaxedCurl, _thumbCurl, t);
        float indexCurl = Mathf.Lerp(_relaxedCurl, _indexCurl, t);
        float middleCurl = Mathf.Lerp(_relaxedCurl, _middleCurl, t);
        float ringCurl = Mathf.Lerp(_relaxedCurl, _ringCurl, t);
        float littleCurl = Mathf.Lerp(_relaxedCurl, _littleCurl, t);

        _hand.Thumb.Curl = thumbCurl;
        _hand.Thumb.Oppose = Mathf.Lerp(0f, _thumbOppose, t);

        _hand.Index.Curl = indexCurl;
        _hand.Middle.Curl = middleCurl;
        _hand.Ring.Curl = ringCurl;
        _hand.Little.Curl = littleCurl;
    }

    // --- API ---

    [Button("Pinch")]
    public void Pinch()
    {
        _target = 1f;
    }

    [Button("Release")]
    public void Release()
    {
        _target = 0f;
    }

    public void SetPinch(float value)
    {
        _target = Mathf.Clamp01(value);
    }
}