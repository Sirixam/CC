using UnityEngine;

public class HandOpenController : MonoBehaviour
{
    [SerializeField] private HandProceduralAnimator _hand;

    [Header("Timing")]
    [SerializeField] private float _speed = 10f;

    [Header("Open Pose")]
    [Range(0f, 1f)][SerializeField] private float _thumbCurl = 0f;
    [Range(0f, 1f)][SerializeField] private float _thumbOppose = 0f;
    [Range(0f, 1f)][SerializeField] private float _indexCurl = 0f;
    [Range(0f, 1f)][SerializeField] private float _middleCurl = 0f;
    [Range(0f, 1f)][SerializeField] private float _ringCurl = 0f;
    [Range(0f, 1f)][SerializeField] private float _littleCurl = 0f;

    [Header("Relaxed Base")]
    [SerializeField] private float _relaxedCurl = 0.15f;

    private float _target;
    private float _current;

    private void Update()
    {
        _current = Mathf.MoveTowards(_current, _target, _speed * Time.deltaTime);
        ApplyOpen(_current);
    }

    private void ApplyOpen(float t)
    {
        _hand.Thumb.Curl = Mathf.Lerp(_relaxedCurl, _thumbCurl, t);
        _hand.Thumb.Oppose = Mathf.Lerp(0f, _thumbOppose, t);
        _hand.Index.Curl = Mathf.Lerp(_relaxedCurl, _indexCurl, t);
        _hand.Middle.Curl = Mathf.Lerp(_relaxedCurl, _middleCurl, t);
        _hand.Ring.Curl = Mathf.Lerp(_relaxedCurl, _ringCurl, t);
        _hand.Little.Curl = Mathf.Lerp(_relaxedCurl, _littleCurl, t);
    }

    [Button("Open")]
    public void Open() => _target = 1f;

    [Button("Relax")]
    public void Relax() => _target = 0f;

    public void SetOpen(float value) => _target = Mathf.Clamp01(value);
}
