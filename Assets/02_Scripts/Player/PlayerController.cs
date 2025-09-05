using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Configurations")]
    [SerializeField] private float _moveSpeed = 5f; // Meters per second
    [SerializeField] private float _lookSpeed = 720f; // Degrees per second

    private Vector3 _movementInput;
    private Vector3 _lookInput;

    private void Awake()
    {
        _inputHandler.ActionEvent += OnActionRequested;
        _inputHandler.DirectionalActionEvent += OnDirectionalActionRequested;
        _inputHandler.HoldActionEvent += OnHoldActionRequested;

        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation; // Prevent tipping over
    }

    private void OnActionRequested(EAction actionType)
    {

    }

    private void OnDirectionalActionRequested(EDirectionalAction actionType, Vector2 input)
    {
        if (actionType == EDirectionalAction.Move)
        {
            _lookInput = new Vector3(input.x, 0, input.y);
            _movementInput = _lookInput;
        }
        else if (actionType == EDirectionalAction.Aim)
        {
            _lookInput = new Vector3(input.x, 0, input.y);
        }
    }

    private void OnHoldActionRequested(EAction actionType, bool isHolding)
    {

    }

    private void Update()
    {
        if (_lookInput != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookInput, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _lookSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        _rigidbody.velocity = _movementInput * _moveSpeed;
    }
}
