using UnityEngine;

public class TeacherManager : MonoBehaviour
{
    public static TeacherManager GetInstance() => FindObjectOfType<TeacherManager>(); // TODO: Remove

    public TeacherController[] TeacherControllers { get; private set; }

    private void Awake()
    {
        TeacherControllers = FindObjectsOfType<TeacherController>();
    }
}
