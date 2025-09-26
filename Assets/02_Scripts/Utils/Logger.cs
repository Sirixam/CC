using UnityEngine;

public enum ELog
{
    None,
    Info,
    Warning,
    Error
}

public class Logger
{
    public static void Log(ELog logType, string message)
    {
        switch (logType)
        {
            case ELog.Info: Debug.Log(message); break;
            case ELog.Warning: Debug.LogWarning(message); break;
            case ELog.Error: Debug.LogError(message); break;
            default: break;
        }
    }
}