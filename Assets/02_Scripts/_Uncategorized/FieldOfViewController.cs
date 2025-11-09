using UnityEngine;

public class FieldOfViewController : MonoBehaviour
{
    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private MeshRenderer _meshRenderer;
    [Header("Configuration")]
    [SerializeField] private float _maxDistance = 5f;
    [SerializeField] private float _fieldOfView = 90f;
    [SerializeField] private float _fieldOfViewWidth = 0.5f;

    private void Awake()
    {
        UpdateMesh();
    }

    public void Show()
    {
        _meshRenderer.enabled = true;
    }

    public void Hide()
    {
        _meshRenderer.enabled = false;
    }

    [Button("Update Mesh")]
    private void UpdateMesh()
    {
        Mesh[] meshes = CreateFieldOfViewMeshes(transform.position, Vector2.up, _maxDistance, _fieldOfView, _fieldOfViewWidth);
        Mesh mesh = MeshUtils.MergeMeshes(meshes);
        _meshFilter.mesh = mesh;
    }

    private Mesh[] CreateFieldOfViewMeshes(Vector3 origin, Vector2 forward, float maxDistance, float fieldOfView, float fieldOfViewWidth)
    {
        Vector3 forwardX0Z = new Vector3(forward.x, 0, forward.y);
        Vector3 forwardLeftX0Z = new Vector3(-forward.y, 0, forward.x);
        Vector3 forawrdRightX0Z = new Vector3(forward.y, 0, -forward.x);

        Vector3 leftOrigin = origin + fieldOfViewWidth * 0.5f * forwardLeftX0Z;
        Vector3 rightOrigin = origin + fieldOfViewWidth * 0.5f * forawrdRightX0Z;
        Vector3 leftForward = leftOrigin + forwardX0Z * maxDistance;
        Vector3 rightForward = rightOrigin + forwardX0Z * maxDistance;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, forwardX0Z);
        Vector2 left2D = MathUtils.GetDirectionFromAngle(-fieldOfView / 2f);
        Vector3 left = new Vector3(left2D.x, 0, left2D.y);
        Vector3 right = new Vector3(-left.x, left.y, left.z);

        Vector3 leftLimit = leftOrigin + rotation * left * maxDistance;
        Vector3 rightLimit = rightOrigin + rotation * right * maxDistance;

        Mesh leftMesh = MeshUtils.CreateCircularTriangleMesh(leftOrigin, leftLimit, leftForward, forward, invertDrawOrder: true);
        Mesh rightMesh = MeshUtils.CreateCircularTriangleMesh(rightOrigin, rightLimit, rightForward, forward, invertDrawOrder: false);
        Mesh centerMesh = MeshUtils.CreateRectangleMesh(leftOrigin, rightOrigin, leftForward, rightForward);
        return new Mesh[] { leftMesh, centerMesh, rightMesh };
    }
}
