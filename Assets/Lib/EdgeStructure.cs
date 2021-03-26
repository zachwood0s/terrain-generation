using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Vertex
{
    public Vector3 v;
    public HalfEdge halfEdge;
    public Triangle triangle;
    public Vertex prevVertex;
    public Vertex nextVertex;

    public bool isReflex;
    public bool isConvex;
    public bool isEar;

    public Vertex(Vector3 pos)
    {
        v = pos;
    }

    public Vector2 Get2D()
    {
        return new Vector2(v.x, v.z);
    }
}

public class Edge 
{
    public Vertex v1;
    public Vertex v2;

    public bool isIntersecting = false;

    public Edge(Vertex _v1, Vertex _v2)
    {
        v1 = _v1;
        v2 = _v2;
    }

    public Vector2 Get2d(Vertex v)
    {
        return new Vector2(v.v.x, v.v.z);
    }

    public void FlipDirection()
    {
        // Swap the vertices
        (v1, v2) = (v2, v1);
    }
}

public class HalfEdge 
{
    public Vertex v;
    public Triangle t;

    public HalfEdge nextEdge;
    public HalfEdge prevEdge;

    public HalfEdge twin;

    public HalfEdge(Vertex _v) 
    {
        v = _v;
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

    public void FlipDirection()
    {
        // Swap the vertices
        (v1, v2) = (v2, v1);
    }

    public IEnumerable<HalfEdge> CollectHalfEdges()
    {
        HalfEdge h1 = new HalfEdge(v1);
        HalfEdge h2 = new HalfEdge(v2);
        HalfEdge h3 = new HalfEdge(v3);

        h1.nextEdge = h2;
        h2.nextEdge = h3;
        h3.nextEdge = h1;

        h1.nextEdge = h3;
        h2.nextEdge = h1;
        h3.nextEdge = h2;

        h1.v.halfEdge = h2;
        h2.v.halfEdge = h3;
        h3.v.halfEdge = h1;

        halfEdge = h1;

        h1.t = this;
        h2.t = this;
        h3.t = this;

        yield return h1;
        yield return h2;
        yield return h3;
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