using System;
using System.Collections.Generic;
using UnityEngine;

public class LookHelper
{
    [Serializable]
    public class Data
    {
        public float LookSpeed = 1080f; // Degrees per second
    }

    private const float FLOAT_TOLERANCE = 0.0001f;

    private readonly Data _data;

    private float _computedLookMultiplier;
    private List<float> _lookMultipliers = new();
    private Vector3 _initialLookDirection;
    private Vector3 _lookDirection;
    private Transform _lookAtPoint;
    private Action _onRotationRestoredCallback;
    private float _aimMultiplier = 1f;

    public LookHelper(Data data)
    {
        _data = data;
        _computedLookMultiplier = 1f;
    }

    public void Initialize(Vector3 lookDirection)
    {
        _initialLookDirection = lookDirection;
        _lookDirection = lookDirection;
    }

    public void SetLookAt(Transform lookAtPoint) => _lookAtPoint = lookAtPoint;

    public void ClearLookAt() => _lookAtPoint = null;

    public void SetLookInput(Vector2 input)
    {
        _lookDirection = new Vector3(input.x, 0, input.y);
    }

    public void UpdateRotation(Transform transform)
    {
        if (_lookAtPoint != null)
        {
            Vector3 lookPosition = _lookAtPoint.position;
            lookPosition.y = transform.position.y;
            _lookDirection = (lookPosition - transform.position).normalized;
        }

        if (_lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookDirection, Vector3.up);
            float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);
        
            // When _aimMultiplier < 1 (aiming), ease into target: fast far away, slow up close
            float easing = 1f;
            if (_aimMultiplier < 1f)
            {
                easing = Mathf.Clamp(angleToTarget / 45f, 0.05f, 1f);
            }
        
            float speed = _data.LookSpeed * _computedLookMultiplier * _aimMultiplier * easing * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, speed);

            if (_onRotationRestoredCallback != null && Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
            {
                var cb = _onRotationRestoredCallback;
                _onRotationRestoredCallback = null;
                cb.Invoke();
            }
        }
    }

    public void RestoreInitialLookDirection(Action onRestored = null)
    {
        _lookDirection = _initialLookDirection;
        _onRotationRestoredCallback = onRestored;
    }

    public void AddLookMultiplier(float value)
    {
        _lookMultipliers.Add(value);
        ComputeLookMultiplier();
    }
    
    public void SetAimMultiplier(float value) => _aimMultiplier = value;

    public void RemoveLookMultiplier(float value)
    {
        for (int i = 0; i < _lookMultipliers.Count; i++)
        {
            if (Mathf.Abs(_lookMultipliers[i] - value) < FLOAT_TOLERANCE)
            {
                _lookMultipliers.RemoveAt(i);
                ComputeLookMultiplier();
                return;
            }
        }
    }

    private void ComputeLookMultiplier()
    {
        _computedLookMultiplier = 1f;
        foreach (var multiplier in _lookMultipliers)
        {
            _computedLookMultiplier *= multiplier;
        }
    }
}
