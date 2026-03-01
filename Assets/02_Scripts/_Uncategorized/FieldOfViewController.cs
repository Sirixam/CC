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
    [SerializeField] [Range(0f, 1f)] private float _widthScale = 1f;
    [SerializeField] private float _visualThickness = 0.01f;
    [SerializeField] private float _physicsThickness = 0.1f;

    private Tween _scaleTween;
    private Vector3 _meshColliderOriginalPosition;
    private bool _isOriginalPositionInitialized;

    private void Awake()
    {
        if (!_isOriginalPositionInitialized)
        {
            _isOriginalPositionInitialized = true;
            _meshColliderOriginalPosition = _meshCollider.transform.localPosition;
        }
        _showTweenSettings.startFromCurrent = true;
        _hideTweenSettings.startFromCurrent = true;
        UpdateMesh();
    }

    public void Show()
    {
        _meshRenderer.enabled = true;
        _meshCollider.transform.localPosition = _meshColliderOriginalPosition;

        _scaleTween.Stop();
        _scaleTween = Tween.Scale(_meshRenderer.transform, _showTweenSettings);
    }

    public void HideInstant()
    {
        if (!_isOriginalPositionInitialized)
        {
            _isOriginalPositionInitialized = true;
            _meshColliderOriginalPosition = _meshCollider.transform.localPosition;
        }
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

    public void SetWidthScale(float scale)
    {
        _widthScale = Mathf.Clamp01(scale);
        UpdateMesh();
    }

    private Mesh GetMesh(float thickness)
    {
        // _widthScale controls the arc sweep (0 = pure rectangle, 1 = full binocular).
        // _fieldOfViewWidth stays constant — it is the bridge/square width, independent of the arc angle.
        return FieldOfViewMeshGenerator.Generate(_maxDistance, _fieldOfView * _widthScale, _fieldOfViewWidth, thickness, segments: 20);

        Mesh[] meshes = CreateFieldOfViewMeshes(transform.localPosition, Vector2.up, _maxDistance, _fieldOfView, _fieldOfViewWidth, thickness);
        return MeshUtils.MergeMeshes(meshes);
    }

    private Mesh[] CreateFieldOfViewMeshes(Vector3 origin, Vector2 forward, float maxDistance, float fieldOfView, float fieldOfViewWidth, float thickness)
    {
        Vector3 forwardX0Z = new Vector3(forward.x, 0, forward.y);
        Vector3 forwardLeftX0Z = new Vector3(-forward.y, 0, forward.x);
        Vector3 forwardRightX0Z = new Vector3(forward.y, 0, -forward.x);

        Vector3 leftOrigin = origin + fieldOfViewWidth * 0.5f * forwardLeftX0Z;
        Vector3 rightOrigin = origin + fieldOfViewWidth * 0.5f * forwardRightX0Z;
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
