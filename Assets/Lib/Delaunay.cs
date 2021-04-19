using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class Delaunay 
{
    private Graph _arr;

    internal Vertex super1, super2;

    private Triangle _graph;

    public Graph Graph => _arr;

    public class Triangle: Face {
        private Vertex _a, _b, _c;

        internal Triangle[] _children;
        private Vertex _v;

        internal bool _generateChildren;

        public IReadOnlyList<Triangle> Children => _children;
        public Vertex A => _a;
        public Vertex B => _b;
        public Vertex C => _c;
        public bool Dummy => Edges().Any(e => e.Dummy);
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
            _generateChildren = true;

            if(this.ToString() == "(118.250000 474.500000), (215.000000 197.000000), (150.500000 382.000000)")
                Debug.Log("Bad");
        }

        public Triangle Flipped() => new Triangle(Boundary[0]) {
            _a = this.A,
            _b = this.C,
            _c = this.B,
        };

        public IEnumerable<HalfEdge> Edges() => Edges(Boundary[0]);
        public IEnumerable<HalfEdge> Edges(HalfEdge starting)
        {
            var e = starting;
            yield return e;
            e = e.Twin.Next;
            yield return e;
            e = e.Twin.Next;
            yield return e;
            if (!Boundary[0].isDead) 
            {
                Assert.IsTrue(ReferenceEquals(e.Head, Boundary[0].Tail), 
                            "Malformed triangle!");
            }

        }

        public override string ToString() => $"{A}, {B}, {C}";
    }

    public Delaunay(Vector2 p0)
    {
        _arr = new Graph();
        super1 = new Vertex(Vector2.zero);
        super1.isDummy = true;
        super2 = new Vertex(Vector2.zero);
        super2.isDummy = true;
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

    public bool IsDummy(Vertex v) => ReferenceEquals(v, super1) || ReferenceEquals(v, super2);
    public bool IsDummy(HalfEdge e) => IsDummy(e.Head) && IsDummy(e.Twin);

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
            //Debug.Log("Took 1");
            return ReferenceEquals(h, super1) || Predicates.YOrder(h.V, p) >= 0;
        }
        if (ReferenceEquals(t, super1))
        {
            //Debug.Log("Took 2");
            // TODO: Potential colinear point problem
            return !ReferenceEquals(h, super2) && Predicates.YOrder(p, h.V) >= 0;
        }
        if (ReferenceEquals(h, super2))
        {
            //Debug.Log("Took 3");
            return Predicates.YOrder(p, t.V) >= 0;
        }
        if (ReferenceEquals(h, super1))
        {
            //Debug.Log("Took 4");
            return Predicates.YOrder(t.V, p) >= 0;
        }
        //Debug.Log("Took 5");
        return Predicates.LeftTurn(p, t.V, h.V) == 1;
    }

    public bool Contains(Vector2 p, Triangle tri) =>
        _LeftOf(p, tri.A, tri.B) && 
        _LeftOf(p, tri.B, tri.C) &&
        _LeftOf(p, tri.C, tri.A);

    public bool OnTri(Vector2 p, Triangle tri) =>
        OnEdge(p, tri.A, tri.B) ||
        OnEdge(p, tri.B, tri.C) ||
        OnEdge(p, tri.C, tri.A) || 
        p == tri.A.V || p == tri.B.V || p == tri.C.V;

    public bool OnEdge(Vector2 p, Vertex a, Vertex b)
    {
        if(IsDummy(a) || IsDummy(b)) return false;

        return Predicates.LeftTurn(p, a.V, b.V) == 0;
    }

    public Triangle Find(Vector2 p)
    {
        Triangle search = _graph;
        while (search.Children[0] != null)
        {
            for (int i = 0; i < 3; ++i)
            {
                var tri = search.Children[i];
                //Debug.Log(tri);
                //Debug.Log($"{_LeftOf(p, tri.A, tri.B)} {_LeftOf(p, tri.B, tri.C)} {_LeftOf(p, tri.C, tri.A)}");
                if(i == 2 || (search.Children[i+1] is null) || Contains(p, search.Children[i]))
                {
                    //Debug.Log($"using child {i}");
                    search = search.Children[i];
                    //Debug.Log(search.ToString());
                    break;
                }
            }
        }
        return search;
    }

    private void _Split(Vertex v, Triangle tri)
    {
        var (e, f, g, _) = tri.Edges();
        /*
        HalfEdge e = tri.Boundary[0];
        HalfEdge f = e.Twin.Next;
        HalfEdge g = f.Twin.Next;
        */
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

    private bool _Flip(HalfEdge edge)
    {
        //Debug.Log($"flipping edge: {e}");
        Triangle tri1 = edge.Face as Triangle, tri2 = edge.Twin.Face as Triangle;
        var (e, f, g, _) = tri1.Edges(edge);
        var (k, h, j, _) = tri2.Edges(edge.Twin);
        /*
        HalfEdge f1 = e.Twin.Next, g1 = f.Twin.Next, h1 = e.Next;
        Debug.Log($"{h} {k} {j}");
        Assert.AreEqual(e, edge);
        Assert.AreEqual(f, f1);
        Assert.AreEqual(g, g1);
        Debug.Log($"{h1}");
        Assert.AreEqual(h, h1);
        */

        Vertex va = e.Tail, vb = f.Tail, vc = g.Tail, vd = h.Head;
        if(Predicates.LeftTurn(vc.V, vd.V, va.V) == 0 || Predicates.LeftTurn(vc.V, vd.V, vb.V) == 0)
        {
            Debug.Log("flipping collinear!");
            return false;
        }

        _arr.RemoveEdge(e);
        HalfEdge i = _AddEdge(vc, vd) ;
        i._nextEdge = g;
        f.Twin._nextEdge = i;
        i.Twin._nextEdge = h.Twin.Next;
        h.Twin._nextEdge = i.Twin;
        tri1._children[0] = tri2._children[0] = new Triangle(i);
        tri1._children[1] = tri2._children[1] = new Triangle(i.Twin);
        tri2._generateChildren = false;
        return true;
    }

    private void _LegalizeEdge(Vertex v, HalfEdge edge)
    {
        //Debug.Log($"legalize edge: {edge}");
        if (!ReferenceEquals(edge.Twin.Next.Head, v))
            edge = edge.Twin;
        if (_Legal(edge))
            return;

        ////Debug.Log($"edge illegal: {edge}");
        HalfEdge f = edge.Next, g = f.Twin.Next;
        if(_Flip(edge))
        {
            _LegalizeEdge(v, f);
            _LegalizeEdge(v, g);
        }
    }


    private bool _Legal(HalfEdge e)
    {
        //Debug.Log($"checking legality: {e}");
        Vertex a = e.Tail, b= e.Head;
        int i = _Index(a), j = _Index(b);

        if (i <= 0 && j <= 0) // Inside the super triangle is always legal
            return true;
        
        Vertex c = e.Twin.Next.Head, d = e.Next.Head;
        int k = _Index(c), l = _Index(d);
        int min_ij = Math.Min(i, j);
        int min_kl = Math.Min(k, l);

        //Debug.Log($"{a}\n{b}\n{c}\n{d}\ni: {i} j: {j} k: {k} l: {l}");

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

    public void Insert(Vector2 p, QualityCheck check=null)
    {
        if (_arr.Contains(p))
        {
            Debug.Log("Identical point, not adding");
            return;
        }
        //Debug.Log($"searching... {p.x} {p.y}");
        Triangle face = Find(p);

        // Make sure the point lands inside the hull if doing quality checks
        if (check != null)
        {
            if (face.Dummy)
            {
                // Point outside the hull. Find offending segment
                var bad = face.Edges().Where(e => !e.Dummy).FirstOrDefault();
                if(bad is null)
                {
                    // Find a better edge
                    float minDist = float.PositiveInfinity;
                    foreach(var search in _arr.Edges)
                    {
                        if(search.Dummy)
                            continue;
                        var closest = GeometryHelpers.FindNearestPointOnLineSegment(search.Tail.V, search.Head.V, p);
                        var dist = (closest - p).sqrMagnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            bad = search;
                        }
                    }
                }

                Debug.Log($"Encroaching! Probably split {bad}");
                check._badSegs.Enqueue(bad.Twin);
                return;
            }
        }

        if (face == null || OnTri(p, face))
        {
            Debug.Log("point on edge, not adding");
            return;
        }

        if (!Contains(p, face))
        {
            Debug.Log($"Point not in found triangle {p}, {face}, not adding");
            return;
        }

        Vertex v = _arr.AddVertex(p);

        //Debug.Log($"found: {face}");
        var (e, f, g, _) = face.Edges();
        /*
        HalfEdge e = face.Boundary[0];
        HalfEdge f = e.Twin.Next;
        HalfEdge g = f.Twin.Next;
        */
        _Split(v, face);
        _LegalizeEdge(v, e);
        _LegalizeEdge(v, f);
        _LegalizeEdge(v, g);
    }

    /// <summary>
    /// Insert a point where the point lies perfectly on an edge
    /// </summary>
    /// <param name="p"></param>
    /// <param name="edge"></param>
    public HalfEdge Insert(Vector2 p, HalfEdge e)
    {
        Assert.IsFalse(IsDummy(e));

        Vertex v = _arr.AddVertex(p);

        Triangle tri1 = e.Face as Triangle, tri2 = e.Twin.Face as Triangle;

        // Create fake faces if necessary
        if (tri1 is null)
        {
            tri1 = new Triangle(e);
        }
        if (tri2 is null)
        {
            tri2 = new Triangle(e.Twin);
        }

        // Split into 4 triangles
        var (_, f, g, _) = tri1.Edges(e);
        var (_, h, i, _) = tri2.Edges(e.Twin);
        var newe = _Split4(v, e, tri1, tri2);

        _LegalizeEdge(v, f);
        _LegalizeEdge(v, g);
        _LegalizeEdge(v, h);
        _LegalizeEdge(v, i);


        return newe;
        /*
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
        */
    }

    private HalfEdge _Split4(Vertex v, HalfEdge edge, Triangle t1, Triangle t2)
    {
        var (e, f, g, _) = t1.Edges(edge);
        var (_, h, i, _) = t2.Edges(edge.Twin);
        HalfEdge f1 = e.Twin.Next, g1 = f.Twin.Next, h1 = e.Next, i1 = h.Twin.Next;
        Assert.AreEqual(f, f1);
        Assert.AreEqual(g, g1);
        Assert.AreEqual(h, h1);
        Assert.AreEqual(i, i1);
        Vertex a = e.Tail, b = f.Tail, c = g.Tail, d = h.Head;
        _arr.RemoveEdge(e);
        // A couple of diagrams to keep it straight in my head
        // verts         vd       edges   h    i      
        // verts    va --v-- vb   edges --- e ---
        // verts         vc       edges   g    f

        // a       b
        //     v
        //     c

        HalfEdge va = _AddEdge(v, a), vb = _AddEdge(v, b), vc = _AddEdge(v, c), vd = _AddEdge(v, d);
        v._halfEdge = va;
        // These go to the right?
        vd._nextEdge = va;
        vb._nextEdge = vd;
        vc._nextEdge = vb;
        va._nextEdge = vc;
        /*
        vd._nextEdge = vb;
        vb._nextEdge = vc;
        vc._nextEdge = va;
        va._nextEdge = vd;
        */
        va.Twin._nextEdge = h;
        vb.Twin._nextEdge = f;
        vc.Twin._nextEdge = g;
        vd.Twin._nextEdge = i;
        /*
        va.Twin._nextEdge = g.Twin;
        vb.Twin._nextEdge = f.Twin;
        vc.Twin._nextEdge = g;
        vd.Twin._nextEdge = i;
        */

        f.Twin._nextEdge = vc.Twin;
        g.Twin._nextEdge = va.Twin;
        h.Twin._nextEdge = vd.Twin;
        i.Twin._nextEdge = vb.Twin;
        t1._children[0] = new Triangle(g);
        t1._children[1] = new Triangle(f);
        t2._children[0] = new Triangle(h);
        t2._children[1] = new Triangle(i);
        /*
        tri._children[0] = new Triangle(e);
        tri._children[1] = new Triangle(f);
        tri._children[2] = new Triangle(g);
        */

        /*
        SPLIT
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
        */

        /*

        FLIP

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
        */
        return va;
    }

    private void LegalizeAll() 
    {
        int safety = 0;
        int flippedEdges = 0;

        while (true)
        {
            safety += 1;
            if (safety > 100000)
            {
                Debug.Log("Stuck in loop");
                break;
            }

            bool hasFlippedEdge = false;

            for (int i = 0; i < _arr.Edges.Count; i++)
            {
                var e = _arr.Edges[i];

                if (e.Twin == null) // Skip boundary edges
                    continue;

                if (!_Legal(e))
                {
                    flippedEdges += 1;
                    hasFlippedEdge = true;
                    _Flip(e);
                }
            }

            if (!hasFlippedEdge)
            {
                Debug.Log("Found triangulation");
                break;
            }
        }
    }

    public void Finish(bool keepAlive)
    {
        // TODO: For some reason, a split triangle will show up in the search tree twice, causing it to be drawn when it shouldn't be.
        void RemoveGraph(Triangle f)
        {
            if (f is null)
                return;

            if (f.Children[0] != null) 
            {
                if (f._generateChildren)
                {
                    foreach(var c in f.Children)
                        RemoveGraph(c);
                }
                return;
            }
            bool notDummy = f.Edges().All(e => !e.Dummy);
            /*
            bool looping = true;
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
            */
            if (notDummy) 
            {
                _arr._faces.Add(f);
                return;
            }

            if(!keepAlive)
            {
                foreach(var edge in f.Edges())
                {
                    edge._face = null;
                }
            }
            /*
            e = f.Boundary[0];
            int count = 0;
            do 
            {
                e._face = null;
                e = e.Twin.Next;
                count++;
                Debug.Log(count);
            } 
            while(!ReferenceEquals(e, f.Boundary[0]));
            */
        }

        void RemoveVertex(Vertex v) 
        {
            /*
            List<HalfEdge> edges = new List<HalfEdge>();
            var e = v.Edge;
            do 
            {
                edges.Add(e);
                e = e.Next;
            }
            while (e != v.Edge);
            */
            var edges = v.Edge.Edges.ToList();

            foreach(var f in edges)
            {
                _arr.RemoveEdge(f);
            }
        }
        _arr._faces.Clear();
        RemoveGraph(_graph);
        if (!keepAlive)
        {
            RemoveVertex(super1);
            RemoveVertex(super2);
        }
    }

    public static Delaunay Generate(List<Vector2> points, bool keepAlive = true)
    {
        //Debug.Log("--Starting--");
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
            if (i == 1489)
            {
                var l = del._arr._vertexSet.ToList();
                l.Sort((x, y) => x.x.CompareTo(y.x));
                Debug.Log("t");
            }
            if (i != imax)
                del.Insert(pt);
        }

        del.Finish(keepAlive);

        return del;
    }


}
