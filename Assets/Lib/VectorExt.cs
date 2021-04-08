using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorExt
{
    public static float Cross(this Vector2 a, Vector2 b) 
    {
        return a.x * b.y - a.y * b.x;
    }
}
