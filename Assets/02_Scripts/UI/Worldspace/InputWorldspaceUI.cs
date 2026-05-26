using UnityEngine;
using UnityEngine.UI;

public class InputWorldspaceUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Vector3 _positionOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private bool _hideOnAwake = true;

    [Header("Overlay")]
    [SerializeField] private bool _useOverlay;
    [SerializeField] private Material _overlayMaterial;
    [SerializeField] private Material _defaultMaterial;

    private Transform _followTarget;
    private Camera _mainCamera;
    private bool _canRotateCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
        ApplyMaterial();
        if (_hideOnAwake)
        {
            gameObject.SetActive(false);
        }
    }

    public void Setup(bool canRotateCamera)
    {
        _canRotateCamera = canRotateCamera;
    }

    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
    }

    public void Show(Sprite sprite)
    {
        _icon.sprite = sprite;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_followTarget != null)
        {
            transform.position = _followTarget.position + _positionOffset;
        }
        if (_canRotateCamera && _mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - _mainCamera.transform.position);
        }
    }

    private void ApplyMaterial()
    {
        if (_useOverlay && _overlayMaterial != null)
            _icon.material = _overlayMaterial;
        else if (!_useOverlay && _defaultMaterial != null)
            _icon.material = _defaultMaterial;
    }
}
