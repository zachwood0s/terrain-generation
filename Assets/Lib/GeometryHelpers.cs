using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public static class GeometryHelpers
{
    public static List<HalfEdge> SetupHalfEdges(List<Triangle> triangles)
    {
        // Orient the triangles the same way
        triangles.ForEach(t => t.MakeClockwise());

        var halfEdges = triangles.SelectMany(t => t.CollectHalfEdges()).ToList();

        foreach(var (i, edge) in halfEdges.WithIndex())
        {
            var endV = edge.v;
            var startV = edge.prevEdge.v;

            foreach(var (j, edge2) in halfEdges.WithIndex())
            {
                if (i == j) continue;

                if (startV.v == edge2.v.v && endV.v == edge2.prevEdge.v.v)
                {
                    edge.twin = edge2;
                    break;
                }
            }
        }

        return halfEdges;
    }

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
        float a = av.x - p.x;
        float d = bv.x - p.x;
        float g = cv.x - p.x;

        float b = av.y - p.y;
        float e = bv.y - p.y;
        float h = cv.y - p.y;

        float c = a * a + b * b;
        float f = d * d + e * e;
        float i = g * g + h * h;

        float determinant = (a * e * i) + (b * f * g) + (c * d * h) - (g * e * c) - (h * f * a) - (i * d * b);

        return determinant;
    }

     
}