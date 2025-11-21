using System;
using UnityEngine;

public class LookHelper
{
    [Serializable]
    public class Data
    {
        public float LookSpeed = 1080f; // Degrees per second
    }

    private readonly Data _data;

    private Vector3 _lookDirection;
    private Transform _lookAtPoint;

    public Vector3 LookDirection => _lookDirection;

    public LookHelper(Data data)
    {
        _data = data;
    }

    public void Initialize(Vector3 startForward)
    {
        _lookDirection = startForward;
    }

    public void SetLookAt(Transform lookAtPoint) => _lookAtPoint = lookAtPoint;

    public void ClearLookAt() => _lookAtPoint = null;

    public void SetLookInput(Vector2 input)
    {
        if (input != Vector2.zero)
        {
            _lookDirection = new Vector3(input.x, 0, input.y);
        }
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
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _data.LookSpeed * Time.deltaTime);
        }
    }
}
