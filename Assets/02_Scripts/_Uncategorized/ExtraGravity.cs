using UnityEngine;

public class ExtraGravity : MonoBehaviour
{
    public float Scale;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        _rb.AddForce(Physics.gravity * Scale, ForceMode.Acceleration);
    }
}