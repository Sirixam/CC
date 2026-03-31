using UnityEngine;

public class PlaneFlightBehavior : MonoBehaviour
{
    private Rigidbody _rb;
    private float _fixedSpeed;
    private Vector3 _direction;
    private bool _hasLanded;

    public void Initialize(float fixedSpeed, Vector3 direction)
    {
        _rb = GetComponent<Rigidbody>();
        _fixedSpeed = fixedSpeed;
        _direction = direction.normalized;
        _rb.useGravity = false;
        _rb.drag = 0;
    }

    private void FixedUpdate()
    {
        if (_hasLanded) return;
        _rb.velocity = _direction * _fixedSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasLanded) return;
        Land(collision);
    }

    public void Land(Collision collision = null)
    {
        _hasLanded = true;
        _rb.useGravity = true;
        _rb.drag = 0.5f;
        _rb.velocity = Vector3.zero;

        // Push away from surface so it doesn't stay embedded
        if (collision != null && collision.contactCount > 0)
        {
            Vector3 normal = collision.GetContact(0).normal;
            _rb.transform.position += normal * 0.05f;
        }
    }
}