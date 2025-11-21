using UnityEngine;

public class StudentNpcController : MonoBehaviour
{
    [SerializeField] private FieldOfViewController _fieldOfViewController;

    [Header("Data")]
    [SerializeField] private LookHelper.Data _lookData;

    // Helpers
    private LookHelper _lookHelper;

    private void Awake()
    {
        _lookHelper = new LookHelper(_lookData);

        // Initialize
        _lookHelper.Initialize(transform.forward);
        _fieldOfViewController.Hide();
    }
}
