using UnityEngine;

public class ExtraGravity : MonoBehaviour
{
  
    public float RiseScale;
    public float FallScale;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        float scale = _rb.velocity.y >= 0f ? RiseScale : FallScale;
        _rb.AddForce(Physics.gravity * scale, ForceMode.Acceleration);
    }
}