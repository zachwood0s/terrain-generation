using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Vertex
{
    public Vector3 v;
    private HalfEdge _halfEdge;
    public Triangle triangle;
    public Vertex prevVertex;
    public Vertex nextVertex;

    public bool isReflex;
    public bool isConvex;
    public bool isEar;

    public HalfEdge Edge => _halfEdge;

    public Vertex(Vector3 pos)
    {
        v = pos;
    }

    public Vector2 Get2D()
    {
        return new Vector2(v.x, v.z);
    }

    public void AddEdge(HalfEdge edge)
    {
        if (_halfEdge is null) 
        {
            _halfEdge = edge;
            edge.nextEdge = edge;
        }
        else if(edge.Clockwise(_halfEdge))
        {
            edge.nextEdge = _halfEdge;
            var last = _halfEdge.Edges.Last();
            last.nextEdge = edge;
            _halfEdge = edge;
        }
        else 
        {                
            var f = _halfEdge.Edges.SkipWhile(x => x.nextEdge.Clockwise(edge)).First();
            edge.nextEdge = f.nextEdge;
            f.nextEdge = edge;
        }
    }

    public void RemoveEdge(HalfEdge e)
    {
        if (ReferenceEquals(_halfEdge, e) && ReferenceEquals(e.nextEdge, e))
        {
            _halfEdge = null;
        }
        else 
        {
            // Find the edge right before the one being removed in the list
            var last = _halfEdge.Edges.SkipWhile(x => !ReferenceEquals(x.nextEdge, e)).First();
            // Reassign to skip the removed edge
            last.nextEdge = e.nextEdge;

            // Reassign the half edge if necessary
            if (ReferenceEquals(_halfEdge, e))
            {
                _halfEdge = last;
            }
        }

    }
}

public class HalfEdge 
{
    private Vertex _tail;
    private Triangle _t;
    private HalfEdge _twin;
    private bool _incomming;

    public HalfEdge nextEdge;

    public Vertex Tail => _tail;
    public Vertex Head => _twin._tail;

    public HalfEdge Twin => _twin;


    public HalfEdge(Vertex _v) 
    {
        _tail = _v;
    }

    public IEnumerable<HalfEdge> Edges {
        get {
            HalfEdge f = this;
            while (f.nextEdge != this) {
                yield return f;
                f = f.nextEdge;
            }
            yield return f;
        }
    }

    public bool Incident(HalfEdge edge) => 
        Tail == edge.Tail || Tail == edge.Head || 
        Head == edge.Tail || Head == edge.Head;

    public bool IncreasingX() => Predicates.XOrder(Tail.v, Head.v) == 1;

    public bool IncreasingY() => Predicates.YOrder(Tail.v, Head.v) == 1;

    public bool Clockwise(HalfEdge edge) 
    {
        bool inc = IncreasingY(), otherInc = edge.IncreasingY();
        return inc != otherInc ? inc : Predicates.LeftTurn(Head.v, Tail.v, edge.Head.v) == 1;
    }

    public bool Outer()
    {
        HalfEdge f = Twin.nextEdge;
        return f != Twin && Predicates.LeftTurn(Tail.v, Head.v, f.Head.v) == 1;
    }
}

public class Triangle 
{
    public Vertex v1;
    public Vertex v2;
    public Vertex v3;

    public HalfEdge halfEdge;

    public Triangle(Vertex _v1, Vertex _v2, Vertex _v3) 
    {
        v1 = _v1;
        v2 = _v2;
        v3 = _v3;
    }

    public Triangle(Vector3 _v1, Vector3 _v2, Vector3 _v3)
    {
        v1 = new Vertex(_v1);
        v2 = new Vertex(_v2);
        v3 = new Vertex(_v3);
    }

    public Triangle(HalfEdge edge)
    {
        halfEdge = edge;
    }

    public void SetVerts(Vertex _v1, Vertex _v2, Vertex _v3)
    {
        v1 = _v1;
        v2 = _v2;
        v3 = _v3;
    }

    public void FlipDirection()
    {
        // Swap the vertices
        (v1, v2) = (v2, v1);
    }

    /// <summary>
    /// Sets up this triangles half edges (if necessary) and returns
    /// each edge in order;
    /// </summary>
    /// <returns></returns>
    public IEnumerable<HalfEdge> CollectHalfEdges()
    {
        /*
        HalfEdge h1, h2, h3;
        if(halfEdge == null)
        {
            h1 = new HalfEdge(v1);
            h2 = new HalfEdge(v2);
            h3 = new HalfEdge(v3);

            h1.nextEdge = h2;
            h2.nextEdge = h3;
            h3.nextEdge = h1;

            h1.nextEdge = h3;
            h2.nextEdge = h1;
            h3.nextEdge = h2;

            h1.Tail.halfEdge = h2;
            h2.Tail.halfEdge = h3;
            h3.Tail.halfEdge = h1;

            halfEdge = h1;

            h1.t = this;
            h2.t = this;
            h3.t = this;

        }
        else {
            h1 = halfEdge;
            h2 = halfEdge.nextEdge;
            h3 = halfEdge.prevEdge;
        }

        yield return h1;
        yield return h2;
        yield return h3;
        */
        return null;
    }

    public void ReassignEdges(HalfEdge h1, HalfEdge h2, HalfEdge h3)
    {
        /*
        h1.t = this;
        h2.t = this;
        h3.t = this;

        halfEdge = h1;

        v1 = h1.tail;
        v2 = h2.tail;
        v3 = h3.tail;
        */
    }

    public void MakeClockwise()
    {
        if (!IsClockwise())
        {
            FlipDirection();
        }
    }

    public bool IsClockwise()
    {
        bool isClockwise = true;

        float determinant = v1.v.x * v2.v.y + v3.v.x * v1.v.y + v2.v.x * v3.v.y - 
                            v1.v.x * v3.v.y - v3.v.x * v2.v.y - v2.v.x * v1.v.y;

        if (determinant > 0f)
        {
            isClockwise = false;
        }

        return isClockwise;
    }
}

public class Face 
{
    public Vector3 pos;
    public Vector3 normal;

    public Face(Vector3 _pos, Vector3 _normal)
    {
        pos = _pos;
        normal = _normal;
    }
}

public class Graph
{
    List<Vertex> vertices;
    List<HalfEdge> edges;
    List<Triangle> triangles;

    IReadOnlyList<Vertex> Vertices => vertices;
    IReadOnlyList<HalfEdge> Edges => edges;
    IReadOnlyList<Triangle> Triangles => triangles;

    public Vertex AddVertex(Vector3 p) 
    {
        var v = new Vertex(p);
        vertices.Add(v);
        return v;
    }

    public HalfEdge AddEdge(Vertex tail, Vertex head)
    {
        var e = AddHalfEdge(tail, null, true);
        var et = AddHalfEdge(head, e, false);
        e.twin = et;
    }

    public HalfEdge AddHalfEdge(Vertex tail, HalfEdge twin, bool incomming)
    {
        var e = new HalfEdge(tail);
        e.twin = twin;
        e.incomming = incomming;
        edges.Add(e);
        return e;
    }
}
