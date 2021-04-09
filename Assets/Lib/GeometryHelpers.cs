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
    /// Find the circumcenter of a triangle.
    /// </summary>
    /// <param name="org">Triangle point.</param>
    /// <param name="dest">Triangle point.</param>
    /// <param name="apex">Triangle point.</param>
    /// <param name="xi">Relative coordinate of new location.</param>
    /// <param name="eta">Relative coordinate of new location.</param>
    /// <param name="offconstant">Off-center constant.</param>
    /// <returns>Coordinates of the circumcenter (or off-center)</returns>
    /// <ref> Pulled from triangle.net </ref>
    public static Vector2 FindCircumcenter(Vector2 org, Vector2 dest, Vector2 apex,
        ref double xi, ref double eta, double offconstant)
    {
        double xdo, ydo, xao, yao;
        double dodist, aodist, dadist;
        double denominator;
        double dx, dy, dxoff, dyoff;

        // Compute the circumcenter of the triangle.
        xdo = dest.x - org.x;
        ydo = dest.y - org.y;
        xao = apex.x - org.x;
        yao = apex.y - org.y;
        dodist = xdo * xdo + ydo * ydo;
        aodist = xao * xao + yao * yao;
        dadist = (dest.x - apex.x) * (dest.x - apex.x) +
                    (dest.y - apex.y) * (dest.y - apex.y);

        denominator = 0.5 / (xdo * yao - xao * ydo);

        dx = (yao * dodist - ydo * aodist) * denominator;
        dy = (xdo * aodist - xao * dodist) * denominator;

        // Find the (squared) length of the triangle's shortest edge.  This
        // serves as a conservative estimate of the insertion radius of the
        // circumcenter's parent. The estimate is used to ensure that
        // the algorithm terminates even if very small angles appear in
        // the input PSLG.
        if ((dodist < aodist) && (dodist < dadist))
        {
            if (offconstant > 0.0)
            {
                // Find the position of the off-center, as described by Alper Ungor.
                dxoff = 0.5 * xdo - offconstant * ydo;
                dyoff = 0.5 * ydo + offconstant * xdo;
                // If the off-center is closer to the origin than the
                // circumcenter, use the off-center instead.
                if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                {
                    dx = dxoff;
                    dy = dyoff;
                }
            }
        }
        else if (aodist < dadist)
        {
            if (offconstant > 0.0)
            {
                dxoff = 0.5 * xao + offconstant * yao;
                dyoff = 0.5 * yao - offconstant * xao;
                // If the off-center is closer to the origin than the
                // circumcenter, use the off-center instead.
                if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                {
                    dx = dxoff;
                    dy = dyoff;
                }
            }
        }
        else
        {
            if (offconstant > 0.0)
            {
                dxoff = 0.5 * (apex.x - dest.x) - offconstant * (apex.y - dest.y);
                dyoff = 0.5 * (apex.y - dest.y) + offconstant * (apex.x - dest.x);
                // If the off-center is closer to the destination than the
                // circumcenter, use the off-center instead.
                if (dxoff * dxoff + dyoff * dyoff <
                    (dx - xdo) * (dx - xdo) + (dy - ydo) * (dy - ydo))
                {
                    dx = xdo + dxoff;
                    dy = ydo + dyoff;
                }
            }
        }

        // To interpolate vertex attributes for the new vertex inserted at
        // the circumcenter, define a coordinate system with a xi-axis,
        // directed from the triangle's origin to its destination, and
        // an eta-axis, directed from its origin to its apex.
        // Calculate the xi and eta coordinates of the circumcenter.
        xi = (yao * dx - xao * dy) * (2.0 * denominator);
        eta = (xdo * dy - ydo * dx) * (2.0 * denominator);

        return new Vector2(org.x + (float) dx, org.y + (float) dy);
    }

    public static bool TriangleEncroaching(Vector2 a, Vector2 b, Vector2 c, float goodAngle)
    {
        var dot = (a.x - c.x) * (b.x - c.x) + 
                  (a.y - c.y) * (b.y - b.y);

        if (dot < 0.0f)
        {
            var target = 2.0 * goodAngle - 1.0;
            return dot * dot >= ((target * target) * 
                ((a.x - c.x) * (a.x - c.x) + 
                 (a.y - c.y) * (a.y - c.y)) * 
                ((b.x - c.x) * (b.x - c.x) +
                 (b.y - c.y) * (b.y - c.y)));
        }
        return false;
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

public static class TriExt
{
    public static float MinAngle(this Delaunay.Triangle tri)
    {
        Vector2 aSide = tri.A.V - tri.B.V, bSide = tri.B.V - tri.C.V, cSide = tri.C.V - tri.A.V;
        float aLen = aSide.sqrMagnitude, bLen = bSide.sqrMagnitude, cLen = cSide.sqrMagnitude;
        float minAngle;

        if (aLen < bLen && aLen < cLen)
        {
            // The edge opposite the c is the shortest
            minAngle = Vector2.Dot(bSide, cSide);
            minAngle = minAngle * minAngle / (bLen * cLen);
        }
        else if (bLen > cLen) 
        {
            // The edge opposite a is the shortest
            minAngle = Vector2.Dot(aSide, cSide);
            minAngle = minAngle * minAngle / (aLen * cLen);
        }
        else 
        {
            // The edge opposite b is the shortest
            minAngle = Vector2.Dot(aSide, bSide);
            minAngle = minAngle * minAngle / (aLen * bLen);
        }
        return minAngle;
    }

    public static float MaxAngle(this Delaunay.Triangle tri)
    {
        Vector2 aSide = tri.A.V - tri.B.V, bSide = tri.B.V - tri.C.V, cSide = tri.C.V - tri.A.V;
        float aLen = aSide.sqrMagnitude, bLen = bSide.sqrMagnitude, cLen = cSide.sqrMagnitude;
        float maxAngle;


        if (aLen > bLen && aLen > cLen)
        {
            // The edge opposite the c is the longest
            maxAngle = (bLen + cLen - aLen) / (2 * Mathf.Sqrt(bLen + cLen));
        }
        else if (bLen > cLen) 
        {
            // The edge opposite a is the longest
            maxAngle = (aLen + cLen - bLen) / (2 * Mathf.Sqrt(aLen + cLen));
        }
        else 
        {
            // The edge opposite b is the longest
            maxAngle = (aLen + bLen - cLen) / (2 * Mathf.Sqrt(aLen + bLen));
        }
        return maxAngle;
    }
}

public static class Predicates
{
    public static int LeftTurn(Vector2 a, Vector2 b, Vector2 c) => (c - b).Cross(a - b).Sign();

    public static int XOrder(Vector2 a, Vector2 b) => (b.x - a.x).Sign();

    public static int YOrder(Vector2 a, Vector2 b) => (b.y - a.y).Sign();

    public static int CCW(Vector2 a, Vector2 b) => (a.Cross(b)).Sign();

    public static int PointInCircumcircle(Vector2 av, Vector2 bv, Vector2 cv, Vector2 p) =>
        GeometryHelpers.CirclePointLocation(av, bv, cv, p).Sign();
}