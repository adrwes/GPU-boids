using UnityEngine;

public static class Extensions
{
    public static float[] ToArray(this Vector3 v)
    {
        return new[] {v.x, v.y, v.z};
    }
}
