using System;
using UnityEngine;

[CreateAssetMenu(fileName = "DEF_Event", menuName = "Definitions/Event")]
public class EventDefinition : ScriptableObject
{
    [Flags]
    public enum ETypes
    {
        None = 0,
        LookAround = 1 << 0,
        OnComplete = 1 << 1,
        Sit = 1 << 2,
    }

    [SerializeField] private ETypes _type;
    [HideIf("HideLookAroundData")]
    [SerializeField] private LookAroundEvent.Data _lookAroundData;

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
            LookAroundEvent.Execute(teacherController, _lookAroundData);
        }
        if (_type.HasFlag(ETypes.Sit))
        {
            isHandled = true;
            SitEvent.Execute(teacherController);
        }
        if (_type.HasFlag(ETypes.OnComplete))
        {
            isHandled = true;
            ExecuteOnComplete(teacherController);
        }

        if (!isHandled)
        {
            Debug.LogError("Event type is not being handled: " + name);
        }
    }

    private void ExecuteOnComplete(IActor actor)
    {
        Debug.Log("Execute On Complete for Actor: " + actor.ID); //TODO

    }
}
