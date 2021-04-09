using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Steiner : MonoBehaviour
{
    // Pulled from Triangle.NET who pulled it from aCute https://www.cise.ufl.edu/~ungor/aCute/algorithm.html

    /// <summary>
    /// Find a new location for a Steiner point.
    /// </summary>
    /// <param name="torg"></param>
    /// <param name="tdest"></param>
    /// <param name="tapex"></param>
    /// <param name="circumcenter"></param>
    /// <param name="xi"></param>
    /// <param name="eta"></param>
    /// <param name="offcenter"></param>
    /// <param name="badotri"></param>
    /// 
  
    #if ADVANCED  /// 
    private Vector2 FindNewLocation(Vertex torg, Vertex tdest, Vertex tapex,
        ref double xi, ref double eta, bool offcenter, Otri badotri)
    {
        double offconstant = 0.0;

        // for calculating the distances of the edges
        double xdo, ydo, xao, yao, xda, yda;
        double dodist, aodist, dadist;
        // for exact calculation
        double denominator;
        double dx, dy, dxoff, dyoff;

        ////////////////////////////// HALE'S VARIABLES //////////////////////////////
        // keeps the difference of coordinates edge 
        double xShortestEdge = 0, yShortestEdge = 0;

        // keeps the square of edge lengths
        double shortestEdgeDist = 0, middleEdgeDist = 0, longestEdgeDist = 0;

        // keeps the vertices according to the angle incident to that vertex in a triangle
        Vector2 smallestAngleCorner, middleAngleCorner, largestAngleCorner;

        // keeps the type of orientation if the triangle
        int orientation = 0;
        // keeps the coordinates of circumcenter of itself and neighbor triangle circumcenter	
        Vector2 myCircumcenter, neighborCircumcenter;

        // keeps if bad triangle is almost good or not
        int almostGood = 0;
        // keeps the cosine of the largest angle
        double cosMaxAngle;
        bool isObtuse; // 1: obtuse 0: nonobtuse
        // keeps the radius of petal
        double petalRadius;
        // for calculating petal center
        double xPetalCtr_1, yPetalCtr_1, xPetalCtr_2, yPetalCtr_2, xPetalCtr, yPetalCtr, xMidOfShortestEdge, yMidOfShortestEdge;
        double dxcenter1, dycenter1, dxcenter2, dycenter2;
        // for finding neighbor
        Otri neighborotri = default(Otri);
        double[] thirdPoint = new double[2];
        //int neighborNotFound = -1;
        // for keeping the vertices of the neighbor triangle
        Vertex neighborvertex_1;
        Vertex neighborvertex_2;
        Vertex neighborvertex_3;
        // dummy variables 
        double xi_tmp = 0, eta_tmp = 0;
        //vertex thirdVertex;
        // for petal intersection
        double vector_x, vector_y, xMidOfLongestEdge, yMidOfLongestEdge, inter_x, inter_y;
        double[] p = new double[5], voronoiOrInter = new double[4];
        bool isCorrect;

        // for vector calculations in perturbation
        double ax, ay, d;
        double pertConst = 0.06; // perturbation constant

        double lengthConst = 1; // used at comparing circumcenter's distance to proposed point's distance
        double justAcute = 1; // used for making the program working for one direction only
        // for smoothing
        int relocated = 0;// used to differentiate between calling the deletevertex and just proposing a steiner point
        double[] newloc = new double[2];   // new location suggested by smoothing
        double origin_x = 0, origin_y = 0; // for keeping torg safe
        Otri delotri; // keeping the original orientation for relocation process
        // keeps the first and second direction suggested points
        double dxFirstSuggestion, dyFirstSuggestion, dxSecondSuggestion, dySecondSuggestion;
        // second direction variables
        double xMidOfMiddleEdge, yMidOfMiddleEdge;

        double minangle;	// in order to make sure that the circumcircle of the bad triangle is greater than petal
        // for calculating the slab
        double linepnt1_x, linepnt1_y, linepnt2_x, linepnt2_y;	// two points of the line
        double line_inter_x = 0, line_inter_y = 0;
        double line_vector_x, line_vector_y;
        double[] line_p = new double[3]; // used for getting the return values of functions related to line intersection
        double[] line_result = new double[4];
        // intersection of slab and the petal
        double petal_slab_inter_x_first, petal_slab_inter_y_first, petal_slab_inter_x_second, petal_slab_inter_y_second, x_1, y_1, x_2, y_2;
        double petal_bisector_x, petal_bisector_y, dist;
        double alpha;
        bool neighborNotFound_first;
        bool neighborNotFound_second;
        ////////////////////////////// END OF HALE'S VARIABLES //////////////////////////////

        // Compute the circumcenter of the triangle.
        xdo = tdest.V.x - torg.V.x;
        ydo = tdest.V.y - torg.V.y;
        xao = tapex.V.x - torg.V.x;
        yao = tapex.V.y - torg.V.y;
        xda = tapex.V.x - tdest.V.x;
        yda = tapex.V.y - tdest.V.y;
        // keeps the square of the distances
        dodist = xdo * xdo + ydo * ydo;
        aodist = xao * xao + yao * yao;
        dadist = (tdest.V.x - tapex.V.x) * (tdest.V.x - tapex.V.x) +
            (tdest.V.y - tapex.V.y) * (tdest.V.y - tapex.V.y);

        denominator = 0.5 / (xdo * yao - xao * ydo);
        // calculate the circumcenter in terms of distance to origin point 
        dx = (yao * dodist - ydo * aodist) * denominator;
        dy = (xdo * aodist - xao * dodist) * denominator;
        // for debugging and for keeping circumcenter to use later
        // coordinate value of the circumcenter
        myCircumcenter = new Vector2(torg.V.x + (float) dx, torg.V.y + (float) dy);

        delotri = badotri; // save for later
        ///////////////// FINDING THE ORIENTATION OF TRIANGLE //////////////////
        // Find the (squared) length of the triangle's shortest edge.  This
        //   serves as a conservative estimate of the insertion radius of the
        //   circumcenter's parent.  The estimate is used to ensure that
        //   the algorithm terminates even if very small angles appear in
        //   the input PSLG. 						
        // find the orientation of the triangle, basically shortest and longest edges
        orientation = LongestShortestEdge(aodist, dadist, dodist);
        //printf("org: (%f,%f), dest: (%f,%f), apex: (%f,%f)\n",torg[0],torg[1],tdest[0],tdest[1],tapex[0],tapex[1]);
        /////////////////////////////////////////////////////////////////////////////////////////////
        // 123: shortest: aodist	// 213: shortest: dadist	// 312: shortest: dodist   //	
        //	middle: dadist 		//	middle: aodist 		//	middle: aodist     //
        //	longest: dodist		//	longest: dodist		//	longest: dadist    //
        // 132: shortest: aodist 	// 231: shortest: dadist 	// 321: shortest: dodist   //
        //	middle: dodist 		//	middle: dodist 		//	middle: dadist     //
        //	longest: dadist		//	longest: aodist		//	longest: aodist    //
        /////////////////////////////////////////////////////////////////////////////////////////////

        switch (orientation)
        {
            case 123: 	// assign necessary information
                /// smallest angle corner: dest
                /// largest angle corner: apex
                xShortestEdge = xao; yShortestEdge = yao;

                shortestEdgeDist = aodist;
                middleEdgeDist = dadist;
                longestEdgeDist = dodist;

                smallestAngleCorner = tdest.V;
                middleAngleCorner = torg.V;
                largestAngleCorner = tapex.V;
                break;

            case 132: 	// assign necessary information
                /// smallest angle corner: dest
                /// largest angle corner: org
                xShortestEdge = xao; yShortestEdge = yao;

                shortestEdgeDist = aodist;
                middleEdgeDist = dodist;
                longestEdgeDist = dadist;

                smallestAngleCorner = tdest.V;
                middleAngleCorner = tapex.V;
                largestAngleCorner = torg.V;

                break;
            case 213: 	// assign necessary information
                /// smallest angle corner: org
                /// largest angle corner: apex
                xShortestEdge = xda; yShortestEdge = yda;

                shortestEdgeDist = dadist;
                middleEdgeDist = aodist;
                longestEdgeDist = dodist;

                smallestAngleCorner = torg.V;
                middleAngleCorner = tdest.V;
                largestAngleCorner = tapex.V;
                break;
            case 231: 	// assign necessary information
                /// smallest angle corner: org
                /// largest angle corner: dest
                xShortestEdge = xda; yShortestEdge = yda;

                shortestEdgeDist = dadist;
                middleEdgeDist = dodist;
                longestEdgeDist = aodist;

                smallestAngleCorner = torg.V;
                middleAngleCorner = tapex.V;
                largestAngleCorner = tdest.V;
                break;
            case 312: 	// assign necessary information
                /// smallest angle corner: apex
                /// largest angle corner: org
                xShortestEdge = xdo; yShortestEdge = ydo;

                shortestEdgeDist = dodist;
                middleEdgeDist = aodist;
                longestEdgeDist = dadist;

                smallestAngleCorner = tapex.V;
                middleAngleCorner = tdest.V;
                largestAngleCorner = torg.V;
                break;
            case 321: 	// assign necessary information
            default: // TODO: is this safe?
                /// smallest angle corner: apex
                /// largest angle corner: dest
                xShortestEdge = xdo; yShortestEdge = ydo;

                shortestEdgeDist = dodist;
                middleEdgeDist = dadist;
                longestEdgeDist = aodist;

                smallestAngleCorner = tapex.V;
                middleAngleCorner = torg.V;
                largestAngleCorner = tdest.V;
                break;

        }// end of switch	
        // check for offcenter condition
        if (offcenter && (offconstant > 0.0))
        {
            // origin has the smallest angle
            if (orientation == 213 || orientation == 231)
            {
                // Find the position of the off-center, as described by Alper Ungor.
                dxoff = 0.5 * xShortestEdge - offconstant * yShortestEdge;
                dyoff = 0.5 * yShortestEdge + offconstant * xShortestEdge;
                // If the off-center is closer to destination than the
                //   circumcenter, use the off-center instead.
                /// doubleLY BAD CASE ///			
                if (dxoff * dxoff + dyoff * dyoff <
                    (dx - xdo) * (dx - xdo) + (dy - ydo) * (dy - ydo))
                {
                    dx = xdo + dxoff;
                    dy = ydo + dyoff;
                }
                /// ALMOST GOOD CASE ///
                else
                {
                    almostGood = 1;
                }
                // destination has the smallest angle	
            }
            else if (orientation == 123 || orientation == 132)
            {
                // Find the position of the off-center, as described by Alper Ungor.
                dxoff = 0.5 * xShortestEdge + offconstant * yShortestEdge;
                dyoff = 0.5 * yShortestEdge - offconstant * xShortestEdge;
                // If the off-center is closer to the origin than the
                //   circumcenter, use the off-center instead.
                /// doubleLY BAD CASE ///
                if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                {
                    dx = dxoff;
                    dy = dyoff;
                }
                /// ALMOST GOOD CASE ///		
                else
                {
                    almostGood = 1;
                }
                // apex has the smallest angle	
            }
            else
            {//orientation == 312 || orientation == 321 
                // Find the position of the off-center, as described by Alper Ungor.
                dxoff = 0.5 * xShortestEdge - offconstant * yShortestEdge;
                dyoff = 0.5 * yShortestEdge + offconstant * xShortestEdge;
                // If the off-center is closer to the origin than the
                //   circumcenter, use the off-center instead.
                /// doubleLY BAD CASE ///
                if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                {
                    dx = dxoff;
                    dy = dyoff;
                }
                /// ALMOST GOOD CASE ///		
                else
                {
                    almostGood = 1;
                }
            }
        }
        // if the bad triangle is almost good, apply our approach
        if (almostGood == 1)
        {

            /// calculate cosine of largest angle	///	
            cosMaxAngle = (middleEdgeDist + shortestEdgeDist - longestEdgeDist) / (2 * Mathf.Sqrt((float) middleEdgeDist) * Mathf.Sqrt((float) shortestEdgeDist));
            if (cosMaxAngle < 0.0)
            {
                // obtuse
                isObtuse = true;
            }
            else if (Mathf.Abs((float) cosMaxAngle - 0.0f) <= Mathf.Epsilon)
            {
                // right triangle (largest angle is 90 degrees)
                isObtuse = true;
            }
            else
            {
                // nonobtuse
                isObtuse = false;
            }
            /// RELOCATION	(LOCAL SMOOTHING) ///
            /// check for possible relocation of one of triangle's points ///				
            relocated = DoSmoothing(delotri, torg, tdest, tapex, ref newloc);
            /// if relocation is possible, delete that vertex and insert a vertex at the new location ///		
            if (relocated > 0)
            {
                dx = newloc[0] - torg.V.x;
                dy = newloc[1] - torg.V.y;
                origin_x = torg.V.x;	// keep for later use
                origin_y = torg.V.y;

                switch (relocated)
                {
                    case 1:
                        //printf("Relocate: (%f,%f)\n", torg[0],torg[1]);			
                        mesh.DeleteVertex(ref delotri);
                        break;
                    case 2:
                        //printf("Relocate: (%f,%f)\n", tdest[0],tdest[1]);			
                        delotri.Lnext();
                        mesh.DeleteVertex(ref delotri);
                        break;
                    case 3:
                        //printf("Relocate: (%f,%f)\n", tapex[0],tapex[1]);						
                        delotri.Lprev();
                        mesh.DeleteVertex(ref delotri);
                        break;
                }
            }
            else
            {
                // calculate radius of the petal according to angle constraint
                // first find the visible region, PETAL
                // find the center of the circle and radius
                // choose minimum angle as the maximum of quality angle and the minimum angle of the bad triangle
                minangle = Math.Acos((middleEdgeDist + longestEdgeDist - shortestEdgeDist) / (2 * Math.Sqrt(middleEdgeDist) * Math.Sqrt(longestEdgeDist))) * 180.0 / Math.PI;
                if (behavior.MinAngle > minangle)
                {
                    minangle = behavior.MinAngle;
                }
                else
                {
                    minangle = minangle + 0.5;
                }
                petalRadius = Math.Sqrt(shortestEdgeDist) / (2 * Math.Sin(minangle * Math.PI / 180.0));
                /// compute two possible centers of the petal ///
                // finding the center
                // first find the middle point of smallest edge
                xMidOfShortestEdge = (middleAngleCorner.x + largestAngleCorner.x) / 2.0;
                yMidOfShortestEdge = (middleAngleCorner.y + largestAngleCorner.y) / 2.0;
                // two possible centers
                xPetalCtr_1 = xMidOfShortestEdge + Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (middleAngleCorner.y -
                    largestAngleCorner.y) / Math.Sqrt(shortestEdgeDist);
                yPetalCtr_1 = yMidOfShortestEdge + Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (largestAngleCorner.x -
                    middleAngleCorner.x) / Math.Sqrt(shortestEdgeDist);

                xPetalCtr_2 = xMidOfShortestEdge - Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (middleAngleCorner.y -
                    largestAngleCorner.y) / Math.Sqrt(shortestEdgeDist);
                yPetalCtr_2 = yMidOfShortestEdge - Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (largestAngleCorner.x -
                    middleAngleCorner.x) / Math.Sqrt(shortestEdgeDist);
                // find the correct circle since there will be two possible circles
                // calculate the distance to smallest angle corner
                dxcenter1 = (xPetalCtr_1 - smallestAngleCorner.x) * (xPetalCtr_1 - smallestAngleCorner.x);
                dycenter1 = (yPetalCtr_1 - smallestAngleCorner.y) * (yPetalCtr_1 - smallestAngleCorner.y);
                dxcenter2 = (xPetalCtr_2 - smallestAngleCorner.x) * (xPetalCtr_2 - smallestAngleCorner.x);
                dycenter2 = (yPetalCtr_2 - smallestAngleCorner.y) * (yPetalCtr_2 - smallestAngleCorner.y);

                // whichever is closer to smallest angle corner, it must be the center
                if (dxcenter1 + dycenter1 <= dxcenter2 + dycenter2)
                {
                    xPetalCtr = xPetalCtr_1; yPetalCtr = yPetalCtr_1;
                }
                else
                {
                    xPetalCtr = xPetalCtr_2; yPetalCtr = yPetalCtr_2;
                }
                /// find the third point of the neighbor triangle  ///
                neighborNotFound_first = GetNeighborsVertex(badotri, middleAngleCorner.x, middleAngleCorner.y,
                            smallestAngleCorner.x, smallestAngleCorner.y, ref thirdPoint, ref neighborotri);
                /// find the circumcenter of the neighbor triangle ///
                dxFirstSuggestion = dx;	// if we cannot find any appropriate suggestion, we use circumcenter
                dyFirstSuggestion = dy;
                /// before checking the neighbor, find the petal and slab intersections ///
                // calculate the intersection point of the petal and the slab lines
                // first find the vector			
                // distance between xmid and petal center			
                dist = Math.Sqrt((xPetalCtr - xMidOfShortestEdge) * (xPetalCtr - xMidOfShortestEdge) + (yPetalCtr - yMidOfShortestEdge) * (yPetalCtr - yMidOfShortestEdge));
                // find the unit vector goes from mid point to petal center			
                line_vector_x = (xPetalCtr - xMidOfShortestEdge) / dist;
                line_vector_y = (yPetalCtr - yMidOfShortestEdge) / dist;
                // find the third point other than p and q
                petal_bisector_x = xPetalCtr + line_vector_x * petalRadius;
                petal_bisector_y = yPetalCtr + line_vector_y * petalRadius;
                alpha = (2.0 * behavior.MaxAngle + minangle - 180.0) * Math.PI / 180.0;
                // rotate the vector cw around the petal center			
                x_1 = petal_bisector_x * Math.Cos(alpha) + petal_bisector_y * Math.Sin(alpha) + xPetalCtr - xPetalCtr * Math.Cos(alpha) - yPetalCtr * Math.Sin(alpha);
                y_1 = -petal_bisector_x * Math.Sin(alpha) + petal_bisector_y * Math.Cos(alpha) + yPetalCtr + xPetalCtr * Math.Sin(alpha) - yPetalCtr * Math.Cos(alpha);
                // rotate the vector ccw around the petal center			
                x_2 = petal_bisector_x * Math.Cos(alpha) - petal_bisector_y * Math.Sin(alpha) + xPetalCtr - xPetalCtr * Math.Cos(alpha) + yPetalCtr * Math.Sin(alpha);
                y_2 = petal_bisector_x * Math.Sin(alpha) + petal_bisector_y * Math.Cos(alpha) + yPetalCtr - xPetalCtr * Math.Sin(alpha) - yPetalCtr * Math.Cos(alpha);
                // we need to find correct intersection point, since there are two possibilities
                // weather it is obtuse/acute the one closer to the minimum angle corner is the first direction
                isCorrect = ChooseCorrectPoint(x_2, y_2, middleAngleCorner.x, middleAngleCorner.y, x_1, y_1, true);
                // make sure which point is the correct one to be considered				
                if (isCorrect)
                {
                    petal_slab_inter_x_first = x_1;
                    petal_slab_inter_y_first = y_1;
                    petal_slab_inter_x_second = x_2;
                    petal_slab_inter_y_second = y_2;
                }
                else
                {
                    petal_slab_inter_x_first = x_2;
                    petal_slab_inter_y_first = y_2;
                    petal_slab_inter_x_second = x_1;
                    petal_slab_inter_y_second = y_1;
                }
                /// choose the correct intersection point ///
                // calculate middle point of the longest edge(bisector)
                xMidOfLongestEdge = (middleAngleCorner.x + smallestAngleCorner.x) / 2.0;
                yMidOfLongestEdge = (middleAngleCorner.y + smallestAngleCorner.y) / 2.0;
                // if there is a neighbor triangle
                if (!neighborNotFound_first)
                {
                    neighborvertex_1 = neighborotri.Org();
                    neighborvertex_2 = neighborotri.Dest();
                    neighborvertex_3 = neighborotri.Apex();
                    // now calculate neighbor's circumcenter which is the voronoi site
                    neighborCircumcenter = predicates.FindCircumcenter(neighborvertex_1, neighborvertex_2, neighborvertex_3,
                        ref xi_tmp, ref eta_tmp);

                    /// compute petal and Voronoi edge intersection ///						
                    // in order to avoid degenerate cases, we need to do a vector based calculation for line		
                    vector_x = (middleAngleCorner.y - smallestAngleCorner.y);//(-y, x)
                    vector_y = smallestAngleCorner.x - middleAngleCorner.x;
                    vector_x = myCircumcenter.x + vector_x;
                    vector_y = myCircumcenter.y + vector_y;
                    // by intersecting bisectors you will end up with the one you want to walk on
                    // then this line and circle should be intersected
                    CircleLineIntersection(myCircumcenter.x, myCircumcenter.y, vector_x, vector_y,
                            xPetalCtr, yPetalCtr, petalRadius, ref p);
                    // we need to find correct intersection point, since line intersects circle twice
                    isCorrect = ChooseCorrectPoint(xMidOfLongestEdge, yMidOfLongestEdge, p[3], p[4],
                                myCircumcenter.x, myCircumcenter.y, isObtuse);
                    // make sure which point is the correct one to be considered
                    if (isCorrect)
                    {
                        inter_x = p[3];
                        inter_y = p[4];
                    }
                    else
                    {
                        inter_x = p[1];
                        inter_y = p[2];
                    }
                    //----------------------hale new first direction: for slab calculation---------------//
                    // calculate the intersection of angle lines and Voronoi
                    linepnt1_x = middleAngleCorner.x;
                    linepnt1_y = middleAngleCorner.y;
                    // vector from middleAngleCorner to largestAngleCorner
                    line_vector_x = largestAngleCorner.x - middleAngleCorner.x;
                    line_vector_y = largestAngleCorner.y - middleAngleCorner.y;
                    // rotate the vector around middleAngleCorner in cw by maxangle degrees				
                    linepnt2_x = petal_slab_inter_x_first;
                    linepnt2_y = petal_slab_inter_y_first;
                    // now calculate the intersection of two lines
                    LineLineIntersection(myCircumcenter.x, myCircumcenter.y, vector_x, vector_y, linepnt1_x, linepnt1_y, linepnt2_x, linepnt2_y, ref line_p);
                    // check if there is a suitable intersection
                    if (line_p[0] > 0.0)
                    {
                        line_inter_x = line_p[1];
                        line_inter_y = line_p[2];
                    }
                    else
                    {
                        // for debugging (to make sure)
                        //printf("1) No intersection between two lines!!!\n");
                        //printf("(%.14f,%.14f) (%.14f,%.14f) (%.14f,%.14f) (%.14f,%.14f)\n",myCircumcenter.x,myCircumcenter.y,vector_x,vector_y,linepnt1_x,linepnt1_y,linepnt2_x,linepnt2_y);
                    }

                    //---------------------------------------------------------------------//
                    /// check if there is a Voronoi vertex between before intersection ///
                    // check if the voronoi vertex is between the intersection and circumcenter
                    PointBetweenPoints(inter_x, inter_y, myCircumcenter.x, myCircumcenter.y,
                            neighborCircumcenter.x, neighborCircumcenter.y, ref voronoiOrInter);

                    /// determine the point to be suggested ///
                    if (p[0] > 0.0)
                    { // there is at least one intersection point
                        // if it is between circumcenter and intersection	
                        // if it returns 1.0 this means we have a voronoi vertex within feasible region
                        if (Math.Abs(voronoiOrInter[0] - 1.0) <= EPS)
                        {
                            //-----------------hale new continues 1------------------//
                            // now check if the line intersection is between cc and voronoi
                            PointBetweenPoints(voronoiOrInter[2], voronoiOrInter[3], myCircumcenter.x, myCircumcenter.y, line_inter_x, line_inter_y, ref line_result);
                            if (Math.Abs(line_result[0] - 1.0) <= EPS && line_p[0] > 0.0)
                            {
                                // check if we can go further by picking the slab line and petal intersection
                                // calculate the distance to the smallest angle corner
                                // check if we create a bad triangle or not
                                if (((smallestAngleCorner.x - petal_slab_inter_x_first) * (smallestAngleCorner.x - petal_slab_inter_x_first) +
                                (smallestAngleCorner.y - petal_slab_inter_y_first) * (smallestAngleCorner.y - petal_slab_inter_y_first) >
                            lengthConst * ((smallestAngleCorner.x - line_inter_x) *
                                    (smallestAngleCorner.x - line_inter_x) +
                                    (smallestAngleCorner.y - line_inter_y) *
                                    (smallestAngleCorner.y - line_inter_y)))
                                    && (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, petal_slab_inter_x_first, petal_slab_inter_y_first))
                                    && MinDistanceToNeighbor(petal_slab_inter_x_first, petal_slab_inter_y_first, ref neighborotri) > MinDistanceToNeighbor(line_inter_x, line_inter_y, ref neighborotri))
                                {
                                    // check the neighbor's vertices also, which one if better
                                    //slab and petal intersection is advised
                                    dxFirstSuggestion = petal_slab_inter_x_first - torg.x;
                                    dyFirstSuggestion = petal_slab_inter_y_first - torg.y;
                                }
                                else
                                { // slab intersection point is further away
                                    if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                    {
                                        // apply perturbation
                                        // find the distance between circumcenter and intersection point
                                        d = Math.Sqrt((line_inter_x - myCircumcenter.x) * (line_inter_x - myCircumcenter.x) +
                                            (line_inter_y - myCircumcenter.y) * (line_inter_y - myCircumcenter.y));
                                        // then find the vector going from intersection point to circumcenter
                                        ax = myCircumcenter.x - line_inter_x;
                                        ay = myCircumcenter.y - line_inter_y;

                                        ax = ax / d;
                                        ay = ay / d;
                                        // now calculate the new intersection point which is perturbated towards the circumcenter
                                        line_inter_x = line_inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                        line_inter_y = line_inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                        if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                        {
                                            // go back to circumcenter
                                            dxFirstSuggestion = dx;
                                            dyFirstSuggestion = dy;
                                        }
                                        else
                                        {
                                            // intersection point is suggested
                                            dxFirstSuggestion = line_inter_x - torg.x;
                                            dyFirstSuggestion = line_inter_y - torg.y;
                                        }
                                    }
                                    else
                                    {// we are not creating a bad triangle
                                        // slab intersection is advised
                                        dxFirstSuggestion = line_result[2] - torg.x;
                                        dyFirstSuggestion = line_result[3] - torg.y;
                                    }
                                }
                                //------------------------------------------------------//
                            }
                            else
                            {
                                /// NOW APPLY A BREADTH-FIRST SEARCH ON THE VORONOI
                                if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, neighborCircumcenter.x, neighborCircumcenter.y))
                                {
                                    // go back to circumcenter
                                    dxFirstSuggestion = dx;
                                    dyFirstSuggestion = dy;
                                }
                                else
                                {
                                    // we are not creating a bad triangle
                                    // neighbor's circumcenter is suggested
                                    dxFirstSuggestion = voronoiOrInter[2] - torg.x;
                                    dyFirstSuggestion = voronoiOrInter[3] - torg.y;
                                }
                            }
                        }
                        else
                        { // there is no voronoi vertex between intersection point and circumcenter
                            //-----------------hale new continues 2-----------------//
                            // now check if the line intersection is between cc and intersection point
                            PointBetweenPoints(inter_x, inter_y, myCircumcenter.x, myCircumcenter.y, line_inter_x, line_inter_y, ref line_result);
                            if (Math.Abs(line_result[0] - 1.0) <= EPS && line_p[0] > 0.0)
                            {
                                // check if we can go further by picking the slab line and petal intersection
                                // calculate the distance to the smallest angle corner
                                if (((smallestAngleCorner.x - petal_slab_inter_x_first) * (smallestAngleCorner.x - petal_slab_inter_x_first) +
                            (smallestAngleCorner.y - petal_slab_inter_y_first) * (smallestAngleCorner.y - petal_slab_inter_y_first) >
                            lengthConst * ((smallestAngleCorner.x - line_inter_x) *
                                    (smallestAngleCorner.x - line_inter_x) +
                                    (smallestAngleCorner.y - line_inter_y) *
                                    (smallestAngleCorner.y - line_inter_y)))
                                    && (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, petal_slab_inter_x_first, petal_slab_inter_y_first))
                                    && MinDistanceToNeighbor(petal_slab_inter_x_first, petal_slab_inter_y_first, ref neighborotri) > MinDistanceToNeighbor(line_inter_x, line_inter_y, ref neighborotri))
                                {
                                    //slab and petal intersection is advised
                                    dxFirstSuggestion = petal_slab_inter_x_first - torg.x;
                                    dyFirstSuggestion = petal_slab_inter_y_first - torg.y;
                                }
                                else
                                { // slab intersection point is further away							
                                    if (IsBadTriangleAngle(largestAngleCorner.x, largestAngleCorner.y, middleAngleCorner.x, middleAngleCorner.y, line_inter_x, line_inter_y))
                                    {
                                        // apply perturbation
                                        // find the distance between circumcenter and intersection point
                                        d = Math.Sqrt((line_inter_x - myCircumcenter.x) * (line_inter_x - myCircumcenter.x) +
                                            (line_inter_y - myCircumcenter.y) * (line_inter_y - myCircumcenter.y));
                                        // then find the vector going from intersection point to circumcenter
                                        ax = myCircumcenter.x - line_inter_x;
                                        ay = myCircumcenter.y - line_inter_y;

                                        ax = ax / d;
                                        ay = ay / d;
                                        // now calculate the new intersection point which is perturbated towards the circumcenter
                                        line_inter_x = line_inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                        line_inter_y = line_inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                        if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                        {
                                            // go back to circumcenter
                                            dxFirstSuggestion = dx;
                                            dyFirstSuggestion = dy;
                                        }
                                        else
                                        {
                                            // intersection point is suggested
                                            dxFirstSuggestion = line_inter_x - torg.x;
                                            dyFirstSuggestion = line_inter_y - torg.y;
                                        }
                                    }
                                    else
                                    {// we are not creating a bad triangle
                                        // slab intersection is advised
                                        dxFirstSuggestion = line_result[2] - torg.x;
                                        dyFirstSuggestion = line_result[3] - torg.y;
                                    }
                                }
                                //------------------------------------------------------//
                            }
                            else
                            {
                                if (IsBadTriangleAngle(largestAngleCorner.x, largestAngleCorner.y, middleAngleCorner.x, middleAngleCorner.y, inter_x, inter_y))
                                {
                                    //printf("testtriangle returned false! bad triangle\n");	
                                    // if it is inside feasible region, then insert v2				
                                    // apply perturbation
                                    // find the distance between circumcenter and intersection point
                                    d = Math.Sqrt((inter_x - myCircumcenter.x) * (inter_x - myCircumcenter.x) +
                                        (inter_y - myCircumcenter.y) * (inter_y - myCircumcenter.y));
                                    // then find the vector going from intersection point to circumcenter
                                    ax = myCircumcenter.x - inter_x;
                                    ay = myCircumcenter.y - inter_y;

                                    ax = ax / d;
                                    ay = ay / d;
                                    // now calculate the new intersection point which is perturbated towards the circumcenter
                                    inter_x = inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                    inter_y = inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                    if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, inter_x, inter_y))
                                    {
                                        // go back to circumcenter
                                        dxFirstSuggestion = dx;
                                        dyFirstSuggestion = dy;
                                    }
                                    else
                                    {
                                        // intersection point is suggested
                                        dxFirstSuggestion = inter_x - torg.x;
                                        dyFirstSuggestion = inter_y - torg.y;
                                    }
                                }
                                else
                                {
                                    // intersection point is suggested
                                    dxFirstSuggestion = inter_x - torg.x;
                                    dyFirstSuggestion = inter_y - torg.y;
                                }
                            }
                        }
                        /// if it is an acute triangle, check if it is a good enough location ///
                        // for acute triangle case, we need to check if it is ok to use either of them
                        if ((smallestAngleCorner.x - myCircumcenter.x) * (smallestAngleCorner.x - myCircumcenter.x) +
                            (smallestAngleCorner.y - myCircumcenter.y) * (smallestAngleCorner.y - myCircumcenter.y) >
                            lengthConst * ((smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                                    (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                                    (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                                    (smallestAngleCorner.y - (dyFirstSuggestion + torg.y))))
                        {
                            // use circumcenter
                            dxFirstSuggestion = dx;
                            dyFirstSuggestion = dy;

                        }// else we stick to what we have found	
                    }// intersection point

                }// if it is on the boundary, meaning no neighbor triangle in this direction, try other direction	

                /// DO THE SAME THING FOR THE OTHER DIRECTION ///
                /// find the third point of the neighbor triangle  ///
                neighborNotFound_second = GetNeighborsVertex(badotri, largestAngleCorner.x, largestAngleCorner.y,
                            smallestAngleCorner.x, smallestAngleCorner.y, ref thirdPoint, ref neighborotri);
                /// find the circumcenter of the neighbor triangle ///
                dxSecondSuggestion = dx;	// if we cannot find any appropriate suggestion, we use circumcenter
                dySecondSuggestion = dy;

                /// choose the correct intersection point ///
                // calculate middle point of the longest edge(bisector)
                xMidOfMiddleEdge = (largestAngleCorner.x + smallestAngleCorner.x) / 2.0;
                yMidOfMiddleEdge = (largestAngleCorner.y + smallestAngleCorner.y) / 2.0;
                // if there is a neighbor triangle
                if (!neighborNotFound_second)
                {
                    neighborvertex_1 = neighborotri.Org();
                    neighborvertex_2 = neighborotri.Dest();
                    neighborvertex_3 = neighborotri.Apex();
                    // now calculate neighbor's circumcenter which is the voronoi site
                    neighborCircumcenter = predicates.FindCircumcenter(neighborvertex_1, neighborvertex_2, neighborvertex_3,
                        ref xi_tmp, ref eta_tmp);

                    /// compute petal and Voronoi edge intersection ///
                    // in order to avoid degenerate cases, we need to do a vector based calculation for line		
                    vector_x = (largestAngleCorner.y - smallestAngleCorner.y);//(-y, x)
                    vector_y = smallestAngleCorner.x - largestAngleCorner.x;
                    vector_x = myCircumcenter.x + vector_x;
                    vector_y = myCircumcenter.y + vector_y;


                    // by intersecting bisectors you will end up with the one you want to walk on
                    // then this line and circle should be intersected
                    CircleLineIntersection(myCircumcenter.x, myCircumcenter.y, vector_x, vector_y,
                            xPetalCtr, yPetalCtr, petalRadius, ref p);

                    // we need to find correct intersection point, since line intersects circle twice
                    // this direction is always ACUTE
                    isCorrect = ChooseCorrectPoint(xMidOfMiddleEdge, yMidOfMiddleEdge, p[3], p[4],
                                myCircumcenter.x, myCircumcenter.y, false/*(isObtuse+1)%2*/);
                    // make sure which point is the correct one to be considered
                    if (isCorrect)
                    {
                        inter_x = p[3];
                        inter_y = p[4];
                    }
                    else
                    {
                        inter_x = p[1];
                        inter_y = p[2];
                    }
                    //----------------------hale new second direction:for slab calculation---------------//			
                    // calculate the intersection of angle lines and Voronoi
                    linepnt1_x = largestAngleCorner.x;
                    linepnt1_y = largestAngleCorner.y;
                    // vector from largestAngleCorner to middleAngleCorner 
                    line_vector_x = middleAngleCorner.x - largestAngleCorner.x;
                    line_vector_y = middleAngleCorner.y - largestAngleCorner.y;
                    // rotate the vector around largestAngleCorner in ccw by maxangle degrees				
                    linepnt2_x = petal_slab_inter_x_second;
                    linepnt2_y = petal_slab_inter_y_second;
                    // now calculate the intersection of two lines
                    LineLineIntersection(myCircumcenter.x, myCircumcenter.y, vector_x, vector_y, linepnt1_x, linepnt1_y, linepnt2_x, linepnt2_y, ref line_p);
                    // check if there is a suitable intersection
                    if (line_p[0] > 0.0)
                    {
                        line_inter_x = line_p[1];
                        line_inter_y = line_p[2];
                    }
                    else
                    {
                        // for debugging (to make sure)
                        //printf("1) No intersection between two lines!!!\n");
                        //printf("(%.14f,%.14f) (%.14f,%.14f) (%.14f,%.14f) (%.14f,%.14f)\n",myCircumcenter.x,myCircumcenter.y,vector_x,vector_y,linepnt1_x,linepnt1_y,linepnt2_x,linepnt2_y);
                    }
                    //---------------------------------------------------------------------//
                    /// check if there is a Voronoi vertex between before intersection ///
                    // check if the voronoi vertex is between the intersection and circumcenter
                    PointBetweenPoints(inter_x, inter_y, myCircumcenter.x, myCircumcenter.y,
                            neighborCircumcenter.x, neighborCircumcenter.y, ref voronoiOrInter);
                    /// determine the point to be suggested ///
                    if (p[0] > 0.0)
                    { // there is at least one intersection point				
                        // if it is between circumcenter and intersection	
                        // if it returns 1.0 this means we have a voronoi vertex within feasible region
                        if (Math.Abs(voronoiOrInter[0] - 1.0) <= EPS)
                        {
                            //-----------------hale new continues 1------------------//
                            // now check if the line intersection is between cc and voronoi
                            PointBetweenPoints(voronoiOrInter[2], voronoiOrInter[3], myCircumcenter.x, myCircumcenter.y, line_inter_x, line_inter_y, ref line_result);
                            if (Math.Abs(line_result[0] - 1.0) <= EPS && line_p[0] > 0.0)
                            {
                                // check if we can go further by picking the slab line and petal intersection
                                // calculate the distance to the smallest angle corner
                                // 						
                                if (((smallestAngleCorner.x - petal_slab_inter_x_second) * (smallestAngleCorner.x - petal_slab_inter_x_second) +
                            (smallestAngleCorner.y - petal_slab_inter_y_second) * (smallestAngleCorner.y - petal_slab_inter_y_second) >
                            lengthConst * ((smallestAngleCorner.x - line_inter_x) *
                                    (smallestAngleCorner.x - line_inter_x) +
                                    (smallestAngleCorner.y - line_inter_y) *
                                    (smallestAngleCorner.y - line_inter_y)))
                                    && (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, petal_slab_inter_x_second, petal_slab_inter_y_second))
                                    && MinDistanceToNeighbor(petal_slab_inter_x_second, petal_slab_inter_y_second, ref neighborotri) > MinDistanceToNeighbor(line_inter_x, line_inter_y, ref neighborotri))
                                {
                                    // slab and petal intersection is advised
                                    dxSecondSuggestion = petal_slab_inter_x_second - torg.x;
                                    dySecondSuggestion = petal_slab_inter_y_second - torg.y;
                                }
                                else
                                { // slab intersection point is further away	
                                    if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                    {
                                        // apply perturbation
                                        // find the distance between circumcenter and intersection point
                                        d = Math.Sqrt((line_inter_x - myCircumcenter.x) * (line_inter_x - myCircumcenter.x) +
                                            (line_inter_y - myCircumcenter.y) * (line_inter_y - myCircumcenter.y));
                                        // then find the vector going from intersection point to circumcenter
                                        ax = myCircumcenter.x - line_inter_x;
                                        ay = myCircumcenter.y - line_inter_y;

                                        ax = ax / d;
                                        ay = ay / d;
                                        // now calculate the new intersection point which is perturbated towards the circumcenter
                                        line_inter_x = line_inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                        line_inter_y = line_inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                        if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                        {
                                            // go back to circumcenter
                                            dxSecondSuggestion = dx;
                                            dySecondSuggestion = dy;
                                        }
                                        else
                                        {
                                            // intersection point is suggested
                                            dxSecondSuggestion = line_inter_x - torg.x;
                                            dySecondSuggestion = line_inter_y - torg.y;

                                        }
                                    }
                                    else
                                    {// we are not creating a bad triangle
                                        // slab intersection is advised
                                        dxSecondSuggestion = line_result[2] - torg.x;
                                        dySecondSuggestion = line_result[3] - torg.y;
                                    }
                                }
                                //------------------------------------------------------//
                            }
                            else
                            {
                                if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, neighborCircumcenter.x, neighborCircumcenter.y))
                                {
                                    // go back to circumcenter
                                    dxSecondSuggestion = dx;
                                    dySecondSuggestion = dy;
                                }
                                else
                                { // we are not creating a bad triangle
                                    // neighbor's circumcenter is suggested
                                    dxSecondSuggestion = voronoiOrInter[2] - torg.x;
                                    dySecondSuggestion = voronoiOrInter[3] - torg.y;
                                }
                            }
                        }
                        else
                        { // there is no voronoi vertex between intersection point and circumcenter
                            //-----------------hale new continues 2-----------------//
                            // now check if the line intersection is between cc and intersection point
                            PointBetweenPoints(inter_x, inter_y, myCircumcenter.x, myCircumcenter.y, line_inter_x, line_inter_y, ref line_result);
                            if (Math.Abs(line_result[0] - 1.0) <= EPS && line_p[0] > 0.0)
                            {
                                // check if we can go further by picking the slab line and petal intersection
                                // calculate the distance to the smallest angle corner
                                if (((smallestAngleCorner.x - petal_slab_inter_x_second) * (smallestAngleCorner.x - petal_slab_inter_x_second) +
                            (smallestAngleCorner.y - petal_slab_inter_y_second) * (smallestAngleCorner.y - petal_slab_inter_y_second) >
                            lengthConst * ((smallestAngleCorner.x - line_inter_x) *
                                    (smallestAngleCorner.x - line_inter_x) +
                                    (smallestAngleCorner.y - line_inter_y) *
                                    (smallestAngleCorner.y - line_inter_y)))
                                    && (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, petal_slab_inter_x_second, petal_slab_inter_y_second))
                                    && MinDistanceToNeighbor(petal_slab_inter_x_second, petal_slab_inter_y_second, ref neighborotri) > MinDistanceToNeighbor(line_inter_x, line_inter_y, ref neighborotri))
                                {
                                    // slab and petal intersection is advised
                                    dxSecondSuggestion = petal_slab_inter_x_second - torg.x;
                                    dySecondSuggestion = petal_slab_inter_y_second - torg.y;
                                }
                                else
                                { // slab intersection point is further away							;
                                    if (IsBadTriangleAngle(largestAngleCorner.x, largestAngleCorner.y, middleAngleCorner.x, middleAngleCorner.y, line_inter_x, line_inter_y))
                                    {
                                        // apply perturbation
                                        // find the distance between circumcenter and intersection point
                                        d = Math.Sqrt((line_inter_x - myCircumcenter.x) * (line_inter_x - myCircumcenter.x) +
                                            (line_inter_y - myCircumcenter.y) * (line_inter_y - myCircumcenter.y));
                                        // then find the vector going from intersection point to circumcenter
                                        ax = myCircumcenter.x - line_inter_x;
                                        ay = myCircumcenter.y - line_inter_y;

                                        ax = ax / d;
                                        ay = ay / d;
                                        // now calculate the new intersection point which is perturbated towards the circumcenter
                                        line_inter_x = line_inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                        line_inter_y = line_inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                        if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                        {
                                            // go back to circumcenter
                                            dxSecondSuggestion = dx;
                                            dySecondSuggestion = dy;
                                        }
                                        else
                                        {
                                            // intersection point is suggested
                                            dxSecondSuggestion = line_inter_x - torg.x;
                                            dySecondSuggestion = line_inter_y - torg.y;
                                        }
                                    }
                                    else
                                    {
                                        // we are not creating a bad triangle
                                        // slab intersection is advised
                                        dxSecondSuggestion = line_result[2] - torg.x;
                                        dySecondSuggestion = line_result[3] - torg.y;
                                    }
                                }
                                //------------------------------------------------------//
                            }
                            else
                            {
                                if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, inter_x, inter_y))
                                {
                                    // if it is inside feasible region, then insert v2				
                                    // apply perturbation
                                    // find the distance between circumcenter and intersection point
                                    d = Math.Sqrt((inter_x - myCircumcenter.x) * (inter_x - myCircumcenter.x) +
                                        (inter_y - myCircumcenter.y) * (inter_y - myCircumcenter.y));
                                    // then find the vector going from intersection point to circumcenter
                                    ax = myCircumcenter.x - inter_x;
                                    ay = myCircumcenter.y - inter_y;

                                    ax = ax / d;
                                    ay = ay / d;
                                    // now calculate the new intersection point which is perturbated towards the circumcenter
                                    inter_x = inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                    inter_y = inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                    if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, inter_x, inter_y))
                                    {
                                        // go back to circumcenter
                                        dxSecondSuggestion = dx;
                                        dySecondSuggestion = dy;
                                    }
                                    else
                                    {
                                        // intersection point is suggested
                                        dxSecondSuggestion = inter_x - torg.x;
                                        dySecondSuggestion = inter_y - torg.y;
                                    }
                                }
                                else
                                {
                                    // intersection point is suggested
                                    dxSecondSuggestion = inter_x - torg.x;
                                    dySecondSuggestion = inter_y - torg.y;
                                }
                            }
                        }

                        /// if it is an acute triangle, check if it is a good enough location ///
                        // for acute triangle case, we need to check if it is ok to use either of them
                        if ((smallestAngleCorner.x - myCircumcenter.x) * (smallestAngleCorner.x - myCircumcenter.x) +
                            (smallestAngleCorner.y - myCircumcenter.y) * (smallestAngleCorner.y - myCircumcenter.y) >
                            lengthConst * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                                    (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                                    (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                                    (smallestAngleCorner.y - (dySecondSuggestion + torg.y))))
                        {
                            // use circumcenter
                            dxSecondSuggestion = dx;
                            dySecondSuggestion = dy;

                        }// else we stick on what we have found	
                    }
                }// if it is on the boundary, meaning no neighbor triangle in this direction, the other direction might be ok	
                if (isObtuse)
                {
                    if (neighborNotFound_first && neighborNotFound_second)
                    {
                        //obtuse: check if the other direction works	
                        if (justAcute * ((smallestAngleCorner.x - (xMidOfMiddleEdge)) *
                            (smallestAngleCorner.x - (xMidOfMiddleEdge)) +
                            (smallestAngleCorner.y - (yMidOfMiddleEdge)) *
                            (smallestAngleCorner.y - (yMidOfMiddleEdge))) >
                            (smallestAngleCorner.x - (xMidOfLongestEdge)) *
                            (smallestAngleCorner.x - (xMidOfLongestEdge)) +
                            (smallestAngleCorner.y - (yMidOfLongestEdge)) *
                            (smallestAngleCorner.y - (yMidOfLongestEdge)))
                        {
                            dx = dxSecondSuggestion;
                            dy = dySecondSuggestion;
                        }
                        else
                        {
                            dx = dxFirstSuggestion;
                            dy = dyFirstSuggestion;
                        }
                    }
                    else if (neighborNotFound_first)
                    {
                        //obtuse: check if the other direction works	
                        if (justAcute * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                                (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                                (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                                (smallestAngleCorner.y - (dySecondSuggestion + torg.y))) >
                                (smallestAngleCorner.x - (xMidOfLongestEdge)) *
                                (smallestAngleCorner.x - (xMidOfLongestEdge)) +
                                (smallestAngleCorner.y - (yMidOfLongestEdge)) *
                                (smallestAngleCorner.y - (yMidOfLongestEdge)))
                        {
                            dx = dxSecondSuggestion;
                            dy = dySecondSuggestion;
                        }
                        else
                        {
                            dx = dxFirstSuggestion;
                            dy = dyFirstSuggestion;
                        }
                    }
                    else if (neighborNotFound_second)
                    {
                        //obtuse: check if the other direction works	
                        if (justAcute * ((smallestAngleCorner.x - (xMidOfMiddleEdge)) *
                                (smallestAngleCorner.x - (xMidOfMiddleEdge)) +
                                (smallestAngleCorner.y - (yMidOfMiddleEdge)) *
                                (smallestAngleCorner.y - (yMidOfMiddleEdge))) >
                                (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                                (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                                (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                                (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)))
                        {
                            dx = dxSecondSuggestion;
                            dy = dySecondSuggestion;
                        }
                        else
                        {
                            dx = dxFirstSuggestion;
                            dy = dyFirstSuggestion;
                        }
                    }
                    else
                    {
                        //obtuse: check if the other direction works	
                        if (justAcute * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                            (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                            (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                            (smallestAngleCorner.y - (dySecondSuggestion + torg.y))) >
                            (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                            (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                            (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                            (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)))
                        {
                            dx = dxSecondSuggestion;
                            dy = dySecondSuggestion;
                        }
                        else
                        {
                            dx = dxFirstSuggestion;
                            dy = dyFirstSuggestion;
                        }
                    }
                }
                else
                { // acute : consider other direction
                    if (neighborNotFound_first && neighborNotFound_second)
                    {
                        //obtuse: check if the other direction works	
                        if (justAcute * ((smallestAngleCorner.x - (xMidOfMiddleEdge)) *
                            (smallestAngleCorner.x - (xMidOfMiddleEdge)) +
                            (smallestAngleCorner.y - (yMidOfMiddleEdge)) *
                            (smallestAngleCorner.y - (yMidOfMiddleEdge))) >
                            (smallestAngleCorner.x - (xMidOfLongestEdge)) *
                            (smallestAngleCorner.x - (xMidOfLongestEdge)) +
                            (smallestAngleCorner.y - (yMidOfLongestEdge)) *
                            (smallestAngleCorner.y - (yMidOfLongestEdge)))
                        {
                            dx = dxSecondSuggestion;
                            dy = dySecondSuggestion;
                        }
                        else
                        {
                            dx = dxFirstSuggestion;
                            dy = dyFirstSuggestion;
                        }
                    }
                    else if (neighborNotFound_first)
                    {
                        //obtuse: check if the other direction works	
                        if (justAcute * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                                (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                                (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                                (smallestAngleCorner.y - (dySecondSuggestion + torg.y))) >
                                (smallestAngleCorner.x - (xMidOfLongestEdge)) *
                                (smallestAngleCorner.x - (xMidOfLongestEdge)) +
                                (smallestAngleCorner.y - (yMidOfLongestEdge)) *
                                (smallestAngleCorner.y - (yMidOfLongestEdge)))
                        {
                            dx = dxSecondSuggestion;
                            dy = dySecondSuggestion;
                        }
                        else
                        {
                            dx = dxFirstSuggestion;
                            dy = dyFirstSuggestion;
                        }
                    }
                    else if (neighborNotFound_second)
                    {
                        //obtuse: check if the other direction works	
                        if (justAcute * ((smallestAngleCorner.x - (xMidOfMiddleEdge)) *
                                (smallestAngleCorner.x - (xMidOfMiddleEdge)) +
                                (smallestAngleCorner.y - (yMidOfMiddleEdge)) *
                                (smallestAngleCorner.y - (yMidOfMiddleEdge))) >
                                (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                                (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                                (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                                (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)))
                        {
                            dx = dxSecondSuggestion;
                            dy = dySecondSuggestion;
                        }
                        else
                        {
                            dx = dxFirstSuggestion;
                            dy = dyFirstSuggestion;
                        }
                    }
                    else
                    {
                        //obtuse: check if the other direction works	
                        if (justAcute * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                            (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                            (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                            (smallestAngleCorner.y - (dySecondSuggestion + torg.y))) >
                            (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                            (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                            (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                            (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)))
                        {
                            dx = dxSecondSuggestion;
                            dy = dySecondSuggestion;
                        }
                        else
                        {
                            dx = dxFirstSuggestion;
                            dy = dyFirstSuggestion;
                        }
                    }

                }// end if obtuse
            }// end of relocation				 
        }// end of almostGood	

        Vector2 circumcenter = new Vector2();

        if (relocated <= 0)
        {
            circumcenter.x = torg.V.x + (float) dx;
            circumcenter.y = torg.V.y + (float) dy;
        }
        else
        {
            circumcenter.x = (float) (origin_x + dx);
            circumcenter.y = (float) (origin_y + dy);
        }
        xi = (yao * dx - xao * dy) * (2.0 * denominator);
        eta = (xdo * dy - ydo * dx) * (2.0 * denominator);

        return circumcenter;
    }
    #endif

}
