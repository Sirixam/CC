using UnityEngine;

public class RepellerTrigger : MonoBehaviour
{
    [SerializeField] private LayerMask _layer;
    [SerializeField] private float _enterImpulse = 4f;
    [SerializeField] private float _stayForce = 6f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValid(other)) return;
        Push(other.attachedRigidbody, _enterImpulse, ForceMode.Impulse);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsValid(other)) return;
        Push(other.attachedRigidbody, _stayForce, ForceMode.Force);
    }

    private bool IsValid(Collider other)
    {
        return (_layer.value & (1 << other.gameObject.layer)) != 0 && other.attachedRigidbody != null;
    }

    private void Push(Rigidbody rb, float force, ForceMode mode)
    {
        Vector3 dir = rb.position - transform.position;
        if (dir == Vector3.zero) dir = Vector3.up;
        else dir.Normalize();
        rb.AddForce(dir * force, mode);
    }
}
