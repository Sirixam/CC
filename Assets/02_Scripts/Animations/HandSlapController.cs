using System;
using UnityEngine;

public class HandSlapController : MonoBehaviour
{
    [SerializeField] private HandSlapMotion _leftMotion;
    [SerializeField] private HandSlapMotion _rightMotion;
    [SerializeField] private HandSlapMotion _frontMotion;
    [SerializeField, Range(0f, 90f)] private float _frontHalfAngle = 45f;

    private bool _isLefty;
    private HandView _leftHand;
    private HandView _rightHand;

    public void Initialize(bool isLefty, HandView leftHand, HandView rightHand)
    {
        _isLefty = isLefty;
        _leftHand = leftHand;
        _rightHand = rightHand;
    }

    public HandView Play(Transform playerTransform, Vector3 targetWorldPos, Action onComplete = null)
    {
        (HandSlapMotion motion, HandView hand) = Select(playerTransform, targetWorldPos);
        motion.Play(onComplete);
        return hand;
    }

    private (HandSlapMotion, HandView) Select(Transform playerTransform, Vector3 targetWorldPos)
    {
        Vector3 dir = targetWorldPos - playerTransform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            return (_frontMotion, _isLefty ? _leftHand : _rightHand);
        dir.Normalize();

        Vector3 forward = playerTransform.forward; forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f) forward.Normalize();

        float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(dir, forward), -1f, 1f)) * Mathf.Rad2Deg;
        if (angle <= _frontHalfAngle)
            return (_frontMotion, _isLefty ? _leftHand : _rightHand);

        Vector3 right = playerTransform.right; right.y = 0f;
        return Vector3.Dot(dir, right) >= 0f
            ? (_rightMotion, _rightHand)
            : (_leftMotion, _leftHand);
    }
}
