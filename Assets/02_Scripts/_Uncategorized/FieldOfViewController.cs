using PrimeTween;
using UnityEngine;

public class FieldOfViewController : MonoBehaviour
{
    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private MeshCollider _meshCollider;
    [Header("Configuration")]
    [SerializeField] private TweenSettings<Vector3> _showTweenSettings;
    [SerializeField] private TweenSettings<Vector3> _hideTweenSettings;
    [SerializeField] private float _maxDistance = 5f;
    [SerializeField] private float _fieldOfView = 90f;
    [SerializeField] private float _fieldOfViewWidth = 0.5f;
    [SerializeField] private float _visualThickness = 0.01f;
    [SerializeField] private float _physicsThickness = 0.1f;

    private Tween _scaleTween;

    private void Awake()
    {
        _showTweenSettings.startFromCurrent = true;
        _hideTweenSettings.startFromCurrent = true;
        UpdateMesh();
    }

    public void Show()
    {
        _meshRenderer.enabled = true;
        _meshCollider.transform.localPosition = Vector3.zero;

        _scaleTween.Stop();
        _scaleTween = Tween.Scale(_meshRenderer.transform, _showTweenSettings);
    }

    public void HideInstant()
    {
        _meshRenderer.transform.localScale = _hideTweenSettings.endValue;
        OnHidden();
    }

    public void Hide()
    {
        _scaleTween.Stop();
        _scaleTween = Tween.Scale(_meshRenderer.transform, _hideTweenSettings).OnComplete(OnHidden);
    }

    private void OnHidden()
    {
        _meshRenderer.enabled = false;
        _meshCollider.transform.localPosition = new Vector3(0, -1000, 0);
    }

    [Button("Update Mesh")]
    private void UpdateMesh()
    {
        _meshFilter.mesh = GetMesh(_visualThickness);
        _meshCollider.sharedMesh = GetMesh(_physicsThickness);
    }

    private Mesh GetMesh(float thickness)
    {
        Mesh[] meshes = CreateFieldOfViewMeshes(transform.localPosition, Vector2.up, _maxDistance, _fieldOfView, _fieldOfViewWidth, thickness);
        return MeshUtils.MergeMeshes(meshes);
    }

    private Mesh[] CreateFieldOfViewMeshes(Vector3 origin, Vector2 forward, float maxDistance, float fieldOfView, float fieldOfViewWidth, float thickness)
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

        Mesh leftMesh = MeshUtils.CreateCircularTriangleMesh3D(leftOrigin, leftLimit, leftForward, forward, invertDrawOrder: true, thickness: thickness);
        Mesh rightMesh = MeshUtils.CreateCircularTriangleMesh3D(rightOrigin, rightLimit, rightForward, forward, invertDrawOrder: false, thickness: thickness);
        if (fieldOfViewWidth > 0)
        {
            Vector3 offset = Vector3.up * thickness;
            Mesh centerMesh = MeshUtils.CreateRectangleMesh2D(leftOrigin + offset, rightOrigin + offset, leftForward + offset, rightForward + offset);
            return new Mesh[] { leftMesh, centerMesh, rightMesh };
        }
        return new Mesh[] { leftMesh, rightMesh };
    }
}
