using System;
using UnityEngine;

[CreateAssetMenu(fileName = "DEF_NavigationEvent", menuName = "Definitions/Navigation Event")]
public class NavigationEventDefinition : ScriptableObject
{
    [Flags]
    public enum ETypes
    {
        None = 0,
        LookAround = 1 << 0,
        OnComplete = 1 << 1,
    }

    [SerializeField] private ETypes _type;
    [HideIf("HideLookAroundData")]
    [SerializeField] private LookAroundEventUtils.Data _lookAroundData;

    private bool HideLookAroundData => !_type.HasFlag(ETypes.LookAround);

    public void Execute(IActor actor)
    {
        if (actor is TeacherController teacherController)
        {
            ExecuteTeacher(teacherController);
        }
    }

    private void ExecuteTeacher(TeacherController teacherController)
    {
        bool isHandled = false;
        if (_type.HasFlag(ETypes.LookAround))
        {
            isHandled = true;
            LookAroundEventUtils.Execute(teacherController, _lookAroundData);
        }
        if (_type.HasFlag(ETypes.OnComplete))
        {
            isHandled = true;
            ExecuteOnComplete(teacherController);
        }

        if (!isHandled)
        {
            Debug.LogError("Navigation event type is not being handled: " + name);
        }
    }

    private void ExecuteOnComplete(IActor actor)
    {
        Debug.Log("Execute On Complete for Actor: " + actor.ID); //TODO

    }
}
