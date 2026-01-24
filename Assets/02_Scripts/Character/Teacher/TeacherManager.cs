using System;
using UnityEngine;

public class TeacherManager : MonoBehaviour
{
    [SerializeField] private NavigationManager _navigationManager;
    [SerializeField] private TeacherController[] _teachers;

    public Action<PlayerController> OnPlayerDetected;
    public Action<IItemController> OnItemDetected;

    private void Awake()
    {
        for (int i = 0; i < _teachers.Length; i++)
        {
            string actorID = IActor.GetTeacherID(i);
            _teachers[i].Inject(_navigationManager);
            _teachers[i].OnPlayerDetected += OnPlayerDetected.Invoke;
            _teachers[i].OnItemDetected += OnItemDetected.Invoke;
        }
    }
}
