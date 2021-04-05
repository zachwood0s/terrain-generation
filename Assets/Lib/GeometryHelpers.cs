using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public static class GeometryHelpers
{
    /*
    public static List<HalfEdge> SetupHalfEdges(List<Triangle> triangles)
    {
        // Orient the triangles the same way
        triangles.ForEach(t => t.MakeClockwise());

        var halfEdges = triangles.SelectMany(t => t.CollectHalfEdges()).ToList();

        foreach(var (i, edge) in halfEdges.WithIndex())
        {
            var endV = edge.tail;
            var startV = edge.prevEdge.tail;

            foreach(var (j, edge2) in halfEdges.WithIndex())
            {
                if (i == j) continue;

                if (startV.v == edge2.tail.v && endV.v == edge2.prevEdge.tail.v)
                {
                    edge.twin = edge2;
                    break;
                }
            }
        }

        return halfEdges;
    }
    */

    /// <summary>
    /// Tests if a point is inside, outside or on a circle; 
    /// </summary>
    /// <param name="av">First triangle point</param>
    /// <param name="bv">Second triangle point</param>
    /// <param name="cv">Third triangle point</param>
    /// <param name="p">Point to test</param>
    /// <returns>Positive if inside, negative if outside, 0 if on</returns>
    public static float CirclePointLocation(Vector2 av, Vector2 bv, Vector2 cv, Vector2 p)
    {
        float a1 = av.x - p.x;
        float b1 = bv.x - p.x;
        float c1 = cv.x - p.x;

        float a2 = av.y - p.y;
        float b2 = bv.y - p.y;
        float c2 = cv.y - p.y;

        float a3 = a1 * a1 + a2 * a2;
        float b3 = b1 * b1 + b2 * b2;
        float c3 = c1 * c1 + c2 * c2;

        //float determinant = (a * e * i) + (b * f * g) + (c * d * h) - (g * e * c) - (h * f * a) - (i * d * b);
        float determinant = a1 * (b2 * c3 - b3 * c2) - a2 * (b1 * c3 - b3 * c1) + a3 * (b1 * c2 - b2 * c1);

        return determinant;
    }
}

public static class FloatExt 
{
    public static int Sign(this float a)
    {
        if (Mathf.Approximately(a, 0.0f))
            return 0;
        if (a > 0.0)
            return 1;
        return -1;
    }
}

public static class Predicates
{
    public static int LeftTurn(Vector2 a, Vector2 b, Vector2 c) => (c - b).Cross(a - b).Sign();

    public static int XOrder(Vector2 a, Vector2 b) => (b.x - a.x).Sign();

    public static int YOrder(Vector2 a, Vector2 b) => (b.y - a.y).Sign();

    public static int CCW(Vector2 a, Vector2 b) => (a.Cross(b)).Sign();

}