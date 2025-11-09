using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtils
{
    /// <param name="zero"> What is the angle of the zero. ( e.g. 90 means UP & 0 means RIGHT ) </param>
    public static float GetAngleFromDirection(Vector2 direction, float zero = 90)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0)
        {
            angle = 360 + angle;
        }
        angle = 360 - angle;

        angle += zero;
        angle %= 360;
        return angle;
    }

    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }

    public static Vector2 GetDirectionFromAngle(float angle)
    {
        return new Vector2(Mathf.Sin(Mathf.Deg2Rad * angle), Mathf.Cos(Mathf.Deg2Rad * angle));
    }

    public static Vector2 GetLeft(Vector2 forward)
    {
        return new Vector2(-forward.y, forward.x);
    }

    public static Vector2 GetRight(Vector2 forward)
    {
        return new Vector2(forward.y, -forward.x);
    }
}
