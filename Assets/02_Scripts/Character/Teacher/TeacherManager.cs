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
            _teachers[i].Inject(_navigationManager);
        }
    }

    private void OnEnable()
    {
        for (int i = 0; i < _teachers.Length; i++)
        {
            _teachers[i].OnPlayerDetected += HandlePlayerDetected;
            _teachers[i].OnItemDetected += HandleItemDetected;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < _teachers.Length; i++)
        {
            _teachers[i].OnPlayerDetected -= HandlePlayerDetected;
            _teachers[i].OnItemDetected -= HandleItemDetected;
        }
    }

    private void HandlePlayerDetected(PlayerController player)
    {
        OnPlayerDetected?.Invoke(player);
    }

    private void HandleItemDetected(IItemController item)
    {
        OnItemDetected?.Invoke(item);
        
    }
    
    public void ResetTeachers()
    {
        for (int i = 0; i < _teachers.Length; i++)
        {
            _teachers[i].ResetTeacher();
        }
    }

    public void StartPatrolling()
    {
        for (int i = 0; i < _teachers.Length; i++)
        {
            _teachers[i].StartPatrolling();
        }
    }
    public void PauseAndLookAt(Transform target, float duration, System.Action onComplete)
    {
        for (int i = 0; i < _teachers.Length; i++)
        {
            _teachers[i].PauseAndLookAt(target, duration, onComplete);
        }
    }

    public void PauseAndFollowTarget(Transform target, System.Action onComplete)
    {
        for (int i = 0; i < _teachers.Length; i++)
        {
            _teachers[i].PauseAndFollowTarget(target, onComplete);
        }
    }
}
