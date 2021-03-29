using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Vertex
{
    internal Vector3 _v;
    internal HalfEdge _halfEdge;

    public Vector3 V => _v;

    public HalfEdge Edge => _halfEdge;

    public Vertex(Vector3 pos)
    {
        _v = pos;
    }

    public Vector2 Get2D()
    {
        return new Vector2(_v.x, _v.z);
    }

    public void AddEdge(HalfEdge edge)
    {
        if (_halfEdge is null) 
        {
            _halfEdge = edge;
            edge._nextEdge = edge;
        }
        else if(edge.Clockwise(_halfEdge))
        {
            edge._nextEdge = _halfEdge;
            var last = _halfEdge.Edges.Last();
            last._nextEdge = edge;
            _halfEdge = edge;
        }
        else 
        {                
            var f = _halfEdge.Edges.SkipWhile(x => x._nextEdge.Clockwise(edge)).First();
            edge._nextEdge = f._nextEdge;
            f._nextEdge = edge;
        }
    }

    public void RemoveEdge(HalfEdge e)
    {
        if (ReferenceEquals(_halfEdge, e) && ReferenceEquals(e._nextEdge, e))
        {
            _halfEdge = null;
        }
        else if(!(_halfEdge is null))
        {
            try {
                // Find the edge right before the one being removed in the list
                var last = _halfEdge.Edges.Where(x => ReferenceEquals(x.Next, e)).First();
                // Reassign to skip the removed edge
                last._nextEdge = e._nextEdge;

                // Reassign the half edge if necessary
                if (ReferenceEquals(_halfEdge, e))
                {
                    _halfEdge = last;
                }
            }
            catch (System.Exception ep)
            {                    
                Debug.Log("Didn't find edge!");
            }
        }
        else 
        {
            Debug.Log("No edges to remove!");
        }

    }
}

public class HalfEdge 
{
    internal Vertex _tail;
    internal Face _face;
    internal HalfEdge _twin;
    internal bool _incomming;

    internal HalfEdge _nextEdge;

    public Vertex Tail => _tail;
    public Vertex Head => _twin._tail;

    public Face Face => _face;

    public HalfEdge Twin => _twin;

    public HalfEdge Next => _nextEdge;


    public HalfEdge(Vertex _v) 
    {
        _tail = _v;
    }

    public IEnumerable<HalfEdge> Edges {
        get {
            HalfEdge f = this;
            while (f._nextEdge != this) {
                yield return f;
                f = f._nextEdge;
            }
            yield return f;
        }
    }

    public bool Incident(HalfEdge edge) => 
        Tail == edge.Tail || Tail == edge.Head || 
        Head == edge.Tail || Head == edge.Head;

    public bool IncreasingX() => Predicates.XOrder(Tail._v, Head._v) == 1;

    public bool IncreasingY() => Predicates.YOrder(Tail._v, Head._v) == 1;

    public bool Clockwise(HalfEdge edge) 
    {
        bool inc = IncreasingY(), otherInc = edge.IncreasingY();
        return inc != otherInc ? inc : Predicates.LeftTurn(Head._v, Tail._v, edge.Head._v) == 1;
    }

    public bool Outer()
    {
        HalfEdge f = Twin._nextEdge;
        return f != Twin && Predicates.LeftTurn(Tail._v, Head._v, f.Head._v) == 1;
    }
}

public class Face 
{
    protected internal List<HalfEdge> _boundary;

    public IReadOnlyList<HalfEdge> Boundary => _boundary;

    public Face()
    {
        _boundary = new List<HalfEdge>();
    }
}

public class Graph
{
    internal List<Vertex> _vertices;
    internal List<HalfEdge> _edges;
    internal List<Face> _faces;

    public IReadOnlyList<Vertex> Vertices => _vertices;
    public IReadOnlyList<HalfEdge> Edges => _edges;
    public IReadOnlyList<Face> Triangles => _faces;

    public Graph()
    {
        _vertices = new List<Vertex>();
        _edges = new List<HalfEdge>();
        _faces = new List<Face>();
    }
    public Vertex AddVertex(Vector3 p) 
    {
        var v = new Vertex(p);
        _vertices.Add(v);
        return v;
    }

    public HalfEdge AddEdge(Vertex tail, Vertex head)
    {
        var e = AddHalfEdge(tail, null, true);
        var et = AddHalfEdge(head, e, false);
        e._twin = et;
        return e;
    }

    public HalfEdge AddHalfEdge(Vertex tail, HalfEdge twin, bool incomming)
    {
        var e = new HalfEdge(tail);
        e._twin = twin;
        e._incomming = incomming;
        _edges.Add(e);
        return e;
    }

    public void RemoveEdge(HalfEdge e)
    {
        _edges.RemoveAll(x => ReferenceEquals(x, e) || ReferenceEquals(x, e.Twin));
        e.Tail.RemoveEdge(e);
        e.Twin.Tail.RemoveEdge(e.Twin);
    }
}
