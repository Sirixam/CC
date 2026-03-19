using System;
using System.Collections;
using UnityEngine;

public interface ILookAroundActor
{
    Transform Pivot { get; } // Head or upper-body transform, normally.
    Coroutine StartCoroutine(IEnumerator routine);
}

public static class LookAroundEvent
{
    [Serializable]
    public class LookStep
    {
        public float Angle;
        public enum EDirection { Left, Right }
        public EDirection Direction;
        public float RotateDuration = 0.25f;
        public float HoldDuration = 0.5f;

        public float SignedAngle => Direction == EDirection.Left ? -Angle : Angle;
    }
    [Serializable]
    public class Data
    {
        public LookStep[] Steps;
        public bool ReturnToOriginalRotation;
        public float ReturnRotateDuration = 0.25f;
    }


    /*
    [Serializable]
    public class Data
    {
        public float[] Angles;
        public Vector2 IntervalDelayThreshold = new Vector2(0.2f, 0.5f);
        public float RotateDuration = 0.25f;
        public bool ReturnToOriginalRotation;

        public float GetRandomIntervalDelay() => UnityEngine.Random.Range(IntervalDelayThreshold.x, IntervalDelayThreshold.y);
    }*/

    public static void Execute(ILookAroundActor actor, Data data)
    {
        actor.StartCoroutine(LookAroundRoutine(actor, data));
    }

    private static IEnumerator LookAroundRoutine(ILookAroundActor actor, Data data)
    {
        Transform pivot = actor.Pivot;
        Quaternion originalRotation = pivot.localRotation;

        foreach (LookStep step in data.Steps)
        {
            yield return RotateTo(pivot, originalRotation * Quaternion.Euler(0f, step.SignedAngle, 0f), step.RotateDuration);
            yield return new WaitForSeconds(step.HoldDuration);
        }

        if (data.ReturnToOriginalRotation)
        {
            yield return RotateTo(pivot, originalRotation, data.ReturnRotateDuration);
        }
    }

    /*
    private static IEnumerator LookAroundRoutine(ILookAroundActor actor, Data data)
    {
        Transform pivot = actor.Pivot;
        Quaternion originalRotation = pivot.localRotation;

        foreach (float angle in data.Angles)
        {
            yield return RotateTo(pivot, originalRotation * Quaternion.Euler(0f, angle, 0f), data.RotateDuration);
            yield return new WaitForSeconds(data.GetRandomIntervalDelay());
        }

        if (data.ReturnToOriginalRotation)
        {
            yield return RotateTo(pivot, originalRotation, data.RotateDuration);
        }
    }*/

    private static IEnumerator RotateTo(Transform target, Quaternion targetRotation, float duration)
    {
        Quaternion start = target.localRotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            target.localRotation = Quaternion.Slerp(start, targetRotation, t);
            yield return null;
        }

        target.localRotation = targetRotation;
    }
}
