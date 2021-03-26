using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Vertex
{
    public Vector3 position;
    public HalfEdge halfEdge;
    public Triangle triangle;
    public Vertex prevVertex;
    public Vertex nextVertex;

    public bool isReflex;
    public bool isConvex;
    public bool isEar;

    public Vertex(Vector3 pos)
    {
        position = pos;
    }

    public Vector2 Get2D()
    {
        return new Vector2(position.x, position.z);
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
        return new Vector2(v.position.x, v.position.z);
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

    public HalfEdge oppositeEdge;

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