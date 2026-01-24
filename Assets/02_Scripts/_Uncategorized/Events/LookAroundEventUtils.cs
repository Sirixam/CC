using System;
using System.Collections;
using UnityEngine;

public interface ILookAroundActor
{
    Transform Pivot { get; } // Head or upper-body transform, normally.
    Coroutine StartCoroutine(IEnumerator routine);
}

public static class LookAroundEventUtils
{
    [Serializable]
    public class Data
    {
        public float[] Angles;
    }

    public static void Execute(ILookAroundActor actor, Data data)
    {
        actor.StartCoroutine(LookAroundRoutine(actor, data.Angles));
    }

    private static IEnumerator LookAroundRoutine(ILookAroundActor actor, params float[] angles)
    {
        Transform pivot = actor.Pivot;
        Quaternion originalRotation = pivot.localRotation;

        foreach (float angle in angles)
        {
            yield return RotateTo(pivot, originalRotation * Quaternion.Euler(0f, angle, 0f), 0.25f);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 0.5f));
        }

        yield return RotateTo(pivot, originalRotation, 0.3f);
    }

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
