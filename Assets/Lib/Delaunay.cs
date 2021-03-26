using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Delaunay 
{

    public static void Generate(HashSet<Vector3> points)
    {
        Assert.IsTrue(points.Count >= 2, "Must have more than 1 point");

        // Step 1: Create super triangle and point location graph

        // Step 2: Insert each point into the graph

        // Step 2.1: Use the graph to find triangle that contains the point

        // Step 2.2: Split it into 3 (or 4) triangles

        // Step 2.3: Call del which removes the illegal edges

        // Step 2.4: Update the graph

        // Step 3: Remove super triangle

    }
}
