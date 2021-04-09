using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Quality 
{
    public float minAngle = 0.0f;
    public float maxAngle = 180.0f;

    public int maxSteiner = -1; // -1 if no cap

    internal float goodAngle;
    internal float maxGoodAngle ;
}
public class Mesher
{
    public static Graph Triangulate(List<Vector2> points, Quality quality)
    {
        var del = Delaunay.Generate(points);
        QualityCheck.Enforce(del, quality);
        del.Finish(false);

        return del.Graph;
    }
}
