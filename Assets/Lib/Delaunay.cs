using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class Delaunay 
{
    private Graph _arr;

    private Vertex super1, super2;

    private Triangle _graph;

    public class Triangle: Face {
        private Vertex _a, _b, _c;

        internal Triangle[] _children;
        private Vertex _v;

        internal bool _flag;

        public IReadOnlyList<Triangle> Children => _children;
        public Vertex A => _a;
        public Vertex B => _b;
        public Vertex C => _c;
        public Triangle(HalfEdge e)
        {
            _boundary.Add(e);

            for (int i = 0; i < 3; ++i, e = e.Twin.Next)
                e._face = this;

            _a = e.Tail;
            _b = e.Head;
            _c = e.Twin.Next.Head;
            _children = new Triangle[3];
            _children[0] = _children[1] = _children[2] = null;
            _flag = true;
        }

        public Triangle Flipped() => new Triangle(Boundary[0]) {
            _a = this.A,
            _b = this.C,
            _c = this.B,
        };

        public override string ToString() => $"{A}, {B}, {C}";
    }

    public Delaunay(Vector2 p0)
    {
        _arr = new Graph();
        super1 = new Vertex(Vector2.zero);
        super2 = new Vertex(Vector2.zero);
        Vertex v0 = _arr.AddVertex(p0);
        // Create the super triangle
        HalfEdge e1 = _AddEdge(super2, super1);
        HalfEdge e2 = _AddEdge(super1, v0);
        HalfEdge e3 = _AddEdge(v0, super2);

        e1._nextEdge = e3.Twin;
        e1.Twin._nextEdge = e2;
        e2._nextEdge = e1.Twin;
        e2.Twin._nextEdge = e3;
        e3._nextEdge = e2.Twin;
        e3.Twin._nextEdge = e1;

        super2._halfEdge = e3.Twin;
        super1._halfEdge = e1.Twin;
        v0._halfEdge = e2.Twin;

        _graph = new Triangle(e1);
    }

    private HalfEdge _AddEdge(Vertex s, Vertex t)
    {
        HalfEdge e1 = _arr.AddHalfEdge(s, null, true);
        HalfEdge e2 = _arr.AddHalfEdge(t, e1, false);
        e1._twin = e2;
        return e1;
    }

    private bool _LeftOf(Vector2 p, Vertex t, Vertex h) 
    {
        if (ReferenceEquals(t, super2)) 
        {
            Debug.Log("Took 1");
            return ReferenceEquals(h, super1) || Predicates.YOrder(h.V, p) >= 0;
        }
        if (ReferenceEquals(t, super1))
        {
            Debug.Log("Took 2");
            // TODO: Potential colinear point problem
            return !ReferenceEquals(h, super2) && Predicates.YOrder(p, h.V) >= 0;
        }
        if (ReferenceEquals(h, super2))
        {
            Debug.Log("Took 3");
            return Predicates.YOrder(p, t.V) >= 0;
        }
        if (ReferenceEquals(h, super1))
        {
            Debug.Log("Took 4");
            return Predicates.YOrder(t.V, p) >= 0;
        }
        Debug.Log("Took 5");
        return Predicates.LeftTurn(p, t.V, h.V) == 1;
    }

    public bool Contains(Vector2 p, Triangle tri)
    {
        return _LeftOf(p, tri.A, tri.B) && 
               _LeftOf(p, tri.B, tri.C) &&
               _LeftOf(p, tri.C, tri.A);
    }

    public Triangle Find(Vector2 p)
    {
        Triangle search = _graph;
        while (search.Children[0] != null)
        {
            for (int i = 0; i < 3; ++i)
            {
                var tri = search.Children[i];
                Debug.Log(tri);
                Debug.Log($"{_LeftOf(p, tri.A, tri.B)} {_LeftOf(p, tri.B, tri.C)} {_LeftOf(p, tri.C, tri.A)}");
                if(i == 2 || (search.Children[i+1] is null) || Contains(p, search.Children[i]))
                {
                    Debug.Log($"using child {i}");
                    search = search.Children[i];
                    Debug.Log(search.ToString());
                    break;
                }
            }
        }
        return search;
    }

    private void _Split(Vertex v, Triangle tri)
    {
        HalfEdge e = tri.Boundary[0];
        HalfEdge f = e.Twin.Next;
        HalfEdge g = f.Twin.Next;
        Vertex a = e.Tail, b = f.Tail, c = g.Tail;
        HalfEdge va = _AddEdge(v, a), vb = _AddEdge(v, b), vc = _AddEdge(v, c);
        v._halfEdge = va;
        vc._nextEdge = vb;
        vb._nextEdge = va;
        va._nextEdge = vc;
        va.Twin._nextEdge = e;
        vb.Twin._nextEdge = f;
        vc.Twin._nextEdge = g;
        e.Twin._nextEdge = vb.Twin;
        f.Twin._nextEdge = vc.Twin;
        g.Twin._nextEdge = va.Twin;
        tri._children[0] = new Triangle(e);
        tri._children[1] = new Triangle(f);
        tri._children[2] = new Triangle(g);
    }

    private void _Flip(HalfEdge e)
    {
        Debug.Log($"flipping edge: {e}");
        Triangle tri1 = e.Face as Triangle, tri2 = e.Twin.Face as Triangle;
        HalfEdge f = e.Twin.Next, g = f.Twin.Next, h = e.Next;
        Vertex va = e.Tail, vb = f.Tail, vc = g.Tail, vd = h.Head;
        _arr.RemoveEdge(e);
        HalfEdge i = _AddEdge(vc, vd) ;
        i._nextEdge = g;
        f.Twin._nextEdge = i;
        i.Twin._nextEdge = h.Twin.Next;
        h.Twin._nextEdge = i.Twin;
        tri1._children[0] = tri2._children[0] = new Triangle(i);
        tri1._children[1] = tri2._children[1] = new Triangle(i.Twin);
        tri2._flag = false;
    }

    private void _LegalizeEdge(Vertex v, HalfEdge edge)
    {
        Debug.Log($"legalize edge: {edge}");
        if (!ReferenceEquals(edge.Twin.Next.Head, v))
            edge = edge.Twin;
        if (_Legal(edge))
            return;

        Debug.Log($"edge illegal: {edge}");
        HalfEdge f = edge.Next, g = f.Twin.Next;
        _Flip(edge);
        _LegalizeEdge(v, f);
        _LegalizeEdge(v, g);
    }

    private bool _Legal(HalfEdge e)
    {
        Debug.Log($"checking legality: {e}");
        Vertex a = e.Tail, b= e.Head;
        int i = _Index(a), j = _Index(b);

        if (i <= 0 && j <= 0) // Inside the super triangle is always legal
            return true;
        
        Vertex c = e.Twin.Next.Head, d = e.Next.Head;
        int k = _Index(c), l = _Index(d);
        int min_ij = Math.Min(i, j);
        int min_kl = Math.Min(k, l);

        Debug.Log($"{a}\n{b}\n{c}\n{d}\ni: {i} j: {j} k: {k} l: {l}");

        if ((min_ij < 0 || min_kl < 0)
                ? min_kl < min_ij 
                : Predicates.PointInCircumcircle(a.V, b.V, c.V, d.V) == -1 )
        {
            return true;
        }

        return _LeftOf(c.V, d, a) || _LeftOf(c.V, b, d);
    }

    private int _Index(Vertex v)
    {
        if (ReferenceEquals(v, super2))
            return -2;
        if (ReferenceEquals(v, super1))
            return -1;
        if (ReferenceEquals(v, _arr.Vertices[0]))
            return 0;
        return 1;
    }


    public void Insert(Vector2 p)
    {
        Vertex v = _arr.AddVertex(p);
        Debug.Log($"searching... {p.x} {p.y}");
        Triangle face = Find(p);
        Debug.Log($"found: {face}");
        HalfEdge e = face.Boundary[0];
        HalfEdge f = e.Twin.Next;
        HalfEdge g = f.Twin.Next;
        _Split(v, face);
        try{
            _LegalizeEdge(v, e);
            _LegalizeEdge(v, f);
            _LegalizeEdge(v, g);
        } catch (StackOverflowException ex)
        {
            Debug.Log("failed to legalize edge");
        }
    }

    private void _Finish()
    {
        void RemoveGraph(Triangle f)
        {
            if (f is null)
                return;

            if (f.Children[0] != null) 
            {
                if (f._flag)
                {
                    foreach(var c in f.Children)
                        RemoveGraph(c);
                }
                return;
            }
            bool looping = true;
            HalfEdge e = f.Boundary[0];
            while(looping)
            {
                looping = !ReferenceEquals(e.Tail, super1) && 
                          !ReferenceEquals(e.Head, super1) && 
                          !ReferenceEquals(e.Tail, super2) &&
                          !ReferenceEquals(e.Head, super2);
                e = e.Twin.Next;
                if (ReferenceEquals(e, f.Boundary[0]))
                    break;
            }
            if (looping) 
            {
                _arr._faces.Add(f);
                return;
            }
            e = f.Boundary[0];

            do 
            {
                e._face = null;
                e = e.Twin.Next;
            } 
            while(!ReferenceEquals(e, f.Boundary[0]));
        }

        void RemoveVertex(Vertex v) 
        {
            List<HalfEdge> edges = new List<HalfEdge>();
            var e = v.Edge;
            do 
            {
                edges.Add(e);
                e = e.Next;
            }
            while (e != v.Edge);

            foreach(var f in edges)
            {
                _arr.RemoveEdge(f);
            }
        }

        RemoveGraph(_graph);
        RemoveVertex(super1);
        RemoveVertex(super2);
    }

    public static Graph Generate(List<Vector2> points)
    {
        Debug.Log("--Starting--");
        // Find the initial point
        int imax = 0;
        foreach (var (i, pt) in points.WithIndex()) 
        {
            if (Predicates.YOrder(points[imax], pt) == 1)        
                imax = i;
        }

        var del = new Delaunay(points[imax]);

        // Insert each point into the graph
        foreach (var (i, pt) in points.WithIndex())
        {
            if (i != imax)
                del.Insert(pt);
        }

        del._Finish();
        return del._arr;
    }


}
