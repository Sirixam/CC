using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GroundFriction : MonoBehaviour
{
    [SerializeField] private float _groundFriction = 10f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _raycastMaxDistance = 1f;

    private Rigidbody _rigidbody;
    private bool _isGrounded;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, _raycastMaxDistance, _groundLayer);
        if (_isGrounded)
        {
            // Apply damping to horizontal velocity only
            Vector3 horizontalVel = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);
            Vector3 counterForce = -horizontalVel * _groundFriction * Time.fixedDeltaTime;
            _rigidbody.AddForce(counterForce, ForceMode.VelocityChange);
        }
    }
}
