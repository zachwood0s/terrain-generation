using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class QualityCheck
{
    private Delaunay _del;
    private Quality _quality;

    internal Queue<HalfEdge> _badSegs;
    private Queue<Delaunay.Triangle> _badTriQueue; // TODO: maybe optimize to use a priority queue
    private int _steinerLeft;
    public QualityCheck(Delaunay del, Quality q)
    {
        _del = del;
        _quality = q;
        _badSegs = new Queue<HalfEdge>();
        _badTriQueue = new Queue<Delaunay.Triangle>();

        _quality.goodAngle = Mathf.Cos(q.minAngle * Mathf.PI / 180.0f);
        _quality.maxGoodAngle = Mathf.Cos(q.maxAngle * Mathf.PI / 180.0f);
        _steinerLeft = _quality.maxSteiner;
    }

    private void _TestTriangle(Delaunay.Triangle tri)
    {
        float maxAngle = tri.MaxAngle(), minAngle = tri.MinAngle();

        if((minAngle < _quality.minAngle) || (maxAngle > _quality.maxGoodAngle && _quality.maxAngle != 0.0f))
        {
            // TODO: for now naively add the triangle. Might need to add checks from Miller, Pav, and Walkington
            _badTriQueue.Enqueue(tri);
        }
    }

    private int _CheckForEncroach(HalfEdge e)
    {
        // Todo add checks for boundary triangles
        int encroached = 0;
        var tri = e.Face as Delaunay.Triangle;

        //if (tri == null) // Boundary triangle
        //    return 0;

        if (tri != null && GeometryHelpers.TriangleEncroaching(tri.A.V, tri.B.V, tri.C.V, _quality.goodAngle))
        {
            encroached = 1;
        }

        //Debug.Log($"{encroached}");

        tri = e.Twin.Face as Delaunay.Triangle;

        //if (tri == null) // Boundary triangle
        //    return 0;

        if (tri != null && GeometryHelpers.TriangleEncroaching(tri.A.V, tri.B.V, tri.C.V, _quality.goodAngle))
        {
            encroached += 2;
        }

        if (encroached > 0)
        {
            if (encroached == 1)
                _badSegs.Enqueue(e);
            else
                _badSegs.Enqueue(e.Twin);
        }

        return encroached;
    }

    private void _TallyEncroaching()
    {
        foreach (var e in _del.Graph.BoundaryEdges)
        {
            _CheckForEncroach(e);
        }
    }

    private void _TallyTriangles()
    {
        foreach (var t in _del.Graph.Triangles.Cast<Delaunay.Triangle>())
        {
            _TestTriangle(t);
        }
    }

    private void _SplitEncroaching()
    {
        Debug.Log($"Fixing {_badSegs.Count} edge(s)");
        while (_badSegs.Count > 0)
        {
            if (_steinerLeft == 0)
                break;

            var seg = _badSegs.Dequeue();
            float split = 0.5f;
            /*
            var encTri = seg.Face as Delaunay.Triangle; 
            var testSeg = seg.Next;
            var testTri = testSeg.Face as Delaunay.Triangle;
            var acuteorg = testSeg is null || _del.IsDummy(testSeg);
            
            // Is the destination shared?
            testTri = testTri?.Boundary[0].Next.Face as Delaunay.Triangle;
            testSeg = testTri?.Boundary[0];
            var acutedest = testSeg is null || _del.IsDummy(testSeg);

            if(seg.Twin != null && !_del.IsDummy(seg.Twin))
            {
                // Todo: check the other segment
            }

            float split = 0.5f;
            if (acuteorg || acutedest)
            {
                var segmentLength = Mathf.Sqrt((seg.Head.V.x - seg.Tail.V.x) * (seg.Head.V.x - seg.Tail.V.x) + 
                                               (seg.Head.V.y - seg.Tail.V.y) * (seg.Head.V.y - seg.Tail.V.y));
                
                // Find the nearest power of two that most evenly splits the segments

                float nearest = 1.0f;
                while (segmentLength > 3.0 * nearest)
                {
                    nearest *= 2.0f;
                }
                while (segmentLength < 1.5 * nearest)
                {
                    nearest *= .5f;
                }
                split = nearest / segmentLength;
                if (acutedest)
                {
                    split = 1.0f - split;
                }
            }
            */

            //Insert vertex;
            Vector2 newPoint = new Vector2(seg.Tail.V.x + split * (seg.Head.V.x - seg.Tail.V.x),
                                           seg.Tail.V.y + split * (seg.Head.V.y - seg.Tail.V.y));

            var newe = _del.Insert(newPoint, seg);
            _CheckForEncroach(newe);
            _CheckForEncroach(newe.Next.Next);

            if (_steinerLeft > 0)
            {
                _steinerLeft--;
            }
            Debug.Log($"Split edge {seg} at {newPoint}");
        }
    }

    private void _SplitTriangle(Delaunay.Triangle tri)
    {
        double xi = 0, eta = 0;
        Vector2 newLoc = GeometryHelpers.FindCircumcenter(tri.A.V, tri.B.V, tri.C.V, ref xi, ref eta, 0.0);
        bool errorOccured = false;

        Debug.Log($"Steiner location {tri} {newLoc}");

        if(newLoc == tri.A.V || newLoc == tri.B.V || newLoc == tri.C.V)
        {
            Debug.Log("Steiner point lands on an existing vertex!");
            errorOccured = true;
        }
        else
        {
            // Todo, might need to check eta against xi
            _del.Insert(newLoc, this);

            if (_steinerLeft > 0)
            {
                _steinerLeft--;
            }
            // Might want to make the delaunay smarter to know if its encroaching or not on insert

        }
        if (errorOccured)
        {
            Debug.Log("Failed: Vertex at the circumcenter of triangle");
            throw new System.Exception("Circumcenter failure");
        }
        Debug.Log("Added steiner point");
    }

    private void _Enforce()
    {
        _TallyEncroaching();
        _SplitEncroaching();

        _del.Finish(true);

        if(_quality.minAngle > 0.0)
        {
            _TallyTriangles();

            Debug.Log($"Fixing {_badTriQueue.Count} triangle(s)");

            while (_badTriQueue.Count > 0 && _steinerLeft != 0)
            {
                var badTri = _badTriQueue.Dequeue();
                _SplitTriangle(badTri);
                _del.Finish(true);
                if (_badSegs.Count > 0)
                {
                    // Will need to try again later
                    //_badTriQueue.Enqueue(badTri); 

                    _SplitEncroaching();
                }
            }
        }
    }

    public static void Enforce(Delaunay del, Quality quality)
    {
        Assert.AreNotEqual(0, del.Graph.Triangles.Count);

        var q = new QualityCheck(del, quality);
        q._Enforce();
    }
}
