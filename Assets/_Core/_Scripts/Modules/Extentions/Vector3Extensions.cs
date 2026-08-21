using System;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Extensions
{
    public static Vector3 WithX(this Vector3 value, float x)
    {
        value.x = x;
        return value;
    }

    public static Vector3 WithY(this Vector3 value, float y)
    {
        value.y = y;
        return value;
    }

    public static Vector3 WithZ(this Vector3 value, float z)
    {
        value.z = z;
        return value;
    }

    public static Vector3 AddX(this Vector3 value, float x)
    {
        value.x += x;
        return value;
    }

    public static Vector3 AddY(this Vector3 value, float y)
    {
        value.y += y;
        return value;
    }

    public static Vector3 AddZ(this Vector3 value, float z)
    {
        value.z += z;
        return value;
    }
    
    public static Vector3 LerpArray(this IReadOnlyList<Vector3> points, float t)
    {
        if (points == null) throw new ArgumentNullException(nameof(points), "Points can't be null");
        if (points.Count < 2) throw new ArgumentException("Can't interpolate between less than 2 elements");

        t = Mathf.Clamp01(t);

        var fragment = 1f / (points.Count - 1);
        var index = Mathf.FloorToInt(t / fragment);
    
        if (index >= points.Count - 1) 
            index = points.Count - 2;

        var lerpT = (t - index * fragment) / fragment;
        var lerp = Vector3.Lerp(points[index], points[index + 1], lerpT);
        return lerp;
    }

    public static Vector3 ToVector3(this Vector3Int vector3Int)
    {
        return vector3Int;
    }
}
