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

    public bool TestTriangle(Delaunay.Triangle tri)
    {
        float maxAngle = tri.MaxAngle(), minAngle = tri.MinAngle();

        if(minAngle < 0f)
        {
            // Colinear Triangle! dissolve it
            //_del.Dissolve(tri);
            //return true;
        }

        if((minAngle < _quality.minAngle) || (maxAngle > _quality.maxGoodAngle && _quality.maxAngle != 0.0f))
        {
            // TODO: for now naively add the triangle. Might need to add checks from Miller, Pav, and Walkington
            /*
            if(!_badTriQueue.Contains(tri)) _badTriQueue.Enqueue(tri);
            return false;
            */
            return false;
        }
        return true;
    }

    private int _CheckForEncroach(HalfEdge e)
    {
        /*
        var tri = e.Face as Delaunay.Triangle;
        if(tri != null && !tri.Dummy && GeometryHelpers.TriangleEncroaching(tri.A.V, tri.B.V, tri.C.V))
        {
            return 1;
        }
        tri = e.Twin.Face as Delaunay.Triangle;
        if(tri != null && !tri.Dummy && GeometryHelpers.TriangleEncroaching(tri.A.V, tri.B.V, tri.C.V))
        {
            return 2;
        }
        return 0;
        */
        
        // Todo add checks for boundary triangles
        int encroached = 0;
        var tri = e.Face as Delaunay.Triangle;

        //if (tri == null) // Boundary triangle
        //    return 0;

        if (tri != null && !tri.Dummy && GeometryHelpers.TriangleEncroaching(tri.A.V, tri.B.V, tri.C.V, _quality.goodAngle))
        {
            encroached = 1;
        }

        //Debug.Log($"{encroached}");

        tri = e.Twin.Face as Delaunay.Triangle;

        //if (tri == null) // Boundary triangle
        //    return 0;

        if (tri != null && !tri.Dummy && GeometryHelpers.TriangleEncroaching(tri.A.V, tri.B.V, tri.C.V, _quality.goodAngle))
        {
            encroached += 2;
        }

        /*
        if (encroached > 0)
        {

            if (encroached == 1)
                _badSegs.Enqueue(e);
            else
                _badSegs.Enqueue(e.Twin);
        }
        */

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
            TestTriangle(t);
        }
    }

    private bool _FindBadEdge(out HalfEdge edge)
    {
        foreach (var e in _del.Graph.BoundaryEdges)
        {
            if(_CheckForEncroach(e) > 0)
            {
                if(!_badSegs.Contains(e))
                {
                    _badSegs.Enqueue(e);
                }
            }
        }
        if(_badSegs.Count > 0)
        {
            edge = _badSegs.Dequeue();
            return true;
        }
        edge = null;
        return false;
    }

    private bool _FindBad(out Delaunay.Triangle tri)
    {
        foreach (var t in _del.Graph.Triangles.Cast<Delaunay.Triangle>())
        {
            if(!TestTriangle(t))
            {
                if(!_badTriQueue.Contains(t))
                {
                    _badTriQueue.Enqueue(t);
                }
            }
        }
        if(_badTriQueue.Count > 0)
        {
            tri = _badTriQueue.Dequeue();
            return true;
        }
        tri = null;
        return false;
    }

    private void _SplitEncroaching(HalfEdge seg, bool recordTriFlaws)
    {
        if(seg.isDead)
            return;

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

        Debug.Log($"Adding {newPoint}");
        var newe = _del.Insert(newPoint, seg, this, recordTriFlaws);

        // Make sure the new subsegments aren't encroaching
        if(newe != null)
        {
            //_CheckForEncroach(newe);
            //_CheckForEncroach(newe.Next.Next);
        }

        if (_steinerLeft > 0)
        {
            _steinerLeft--;
        }
    }

    private void _SplitEncroaching(bool recordTriFlaws)
    {
        Debug.Log($"Fixing {_badSegs.Count} edge(s)");
        while (_badSegs.Count > 0)
        {
            _SplitEncroaching(_badSegs.Dequeue(), recordTriFlaws);
        }
    }

    private bool _EncroachingAny(Vector2 p, out HalfEdge seg)
    {
        foreach(var e in _del.Graph._edges.Where(e => !e.Dummy))
        {
            if(GeometryHelpers.PointEncroaching(e.Tail.V, e.Head.V, p))
            {
                seg = e;
                return true;
            }
        }
        seg = null;
        return false;
    }

    private void _SplitTriangle(Delaunay.Triangle tri)
    {
        if(tri.dead || !_del.Graph.Triangles.Contains(tri))
        {
            Debug.Log("Triangle died somewhere along the way, skipping");
            return;
        }
        double xi = 0, eta = 0;
        Vector2 newLoc = GeometryHelpers.FindCircumcenter(tri.A.V, tri.B.V, tri.C.V, ref xi, ref eta, 0.0);
        bool errorOccured = false;

        if(newLoc == tri.A.V || newLoc == tri.B.V || newLoc == tri.C.V)
        {
            Debug.Log("Steiner point lands on an existing vertex!");
            errorOccured = true;
        }
        else if(_EncroachingAny(newLoc, out var seg)) 
        {
            Debug.Log($"New point {newLoc} would encroach {seg}");
            _badSegs.Enqueue(seg);
        }
        else
        {
            // Todo, might need to check eta against xi
            Debug.Log($"Adding {newLoc}");
            _del.Insert(newLoc, this, true);

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
    }

    private void _Enforce()
    {
        /*
        _TallyEncroaching();
        _SplitEncroaching(false);

        if(_quality.minAngle > 0.0)
        {
            _TallyTriangles();
            */

            /* 
            while (_badTriQueue.Count > 0 && _steinerLeft != 0)
            {
                Debug.Log($"Fixing {_badTriQueue.Count} more triangle(s)");
                var badTri = _badTriQueue.Dequeue();
                _SplitTriangle(badTri);
                if (_badSegs.Count > 0)
                {
                    // Will need to try again later
                    //_badTriQueue.Enqueue(badTri); 

                    _SplitEncroaching(false);
                    _TallyTriangles();
                }
            }
            */
            Delaunay.Triangle badTri = null;
            HalfEdge badEdge = null;
            while((_FindBadEdge(out badEdge) || _FindBad(out badTri)) && _steinerLeft != 0)
            {
                if(badEdge != null)
                    _SplitEncroaching(badEdge, false);
                else
                    _SplitTriangle(badTri);
                
                _del.Finish(true);
            }
        //}

        if(!_FindBad(out badTri))
        {
            Assert.IsFalse(_del.Graph.Triangles.Cast<Delaunay.Triangle>().Any(x => x.MinAngle() < _quality.minAngle));
        }
        else if(_steinerLeft == 0)
        {
            Debug.Log("Used all availible points");
        }
    }

    public static void Enforce(Delaunay del, Quality quality)
    {
        Assert.AreNotEqual(0, del.Graph.Triangles.Count);

        var q = new QualityCheck(del, quality);
        q._Enforce();
    }
}
