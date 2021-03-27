using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Delaunay 
{

    public static Triangle SuperTriangle() => 
        new Triangle(new Vector3(-100f, 0, -100f), new Vector3(100f, 0, -100f), new Vector3(0, 0, 100f));

    public static void Generate(HashSet<Vector3> points)
    {
        Assert.IsTrue(points.Count >= 2, "Must have more than 1 point");

        // Step 1: Create super triangle and point location graph

        var super = SuperTriangle();

        // Step 2: Insert each point into the graph

        // Step 2.1: Use the graph to find triangle that contains the point

        // Step 2.2: Split it into 3 (or 4) triangles

        // Step 2.3: Call del which removes the illegal edges

        // Step 2.4: Update the graph

        // Step 3: Remove super triangle

    }

    public static void FlipEdge(HalfEdge edge)
    {
        var (first, second, third, _) = edge.t.CollectHalfEdges();
        var (fourth, fifth, sixth, _) = edge.twin.t.CollectHalfEdges();

        Vertex a = first.tail;
        Vertex b = second.tail;
        Vertex c = third.tail;
        Vertex d = fifth.tail;

        // Change the vertices
        a.halfEdge = second;
        c.halfEdge = fifth;

        first.SetNextPrev(third, fifth);
        second.SetNextPrev(fourth, sixth);
        third.SetNextPrev(fifth, first);
        fourth.SetNextPrev(sixth, second);
        fifth.SetNextPrev(first, third);
        sixth.SetNextPrev(second, fourth);

        first.tail = b;
        second.tail = b;
        third.tail = c;
        fourth.tail = d;
        fifth.tail = d;
        sixth.tail = a;

        Triangle t1 = first.t;
        Triangle t2 = fourth.t;

        t1.ReassignEdges(first, third, fifth);
        t2.ReassignEdges(second, fourth, fifth);
    }
}
