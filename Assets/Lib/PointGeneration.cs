using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PointGeneration
{

    /// <summary>
    /// Returns an enumarable amount of points distributed based on the provided distribution map.
    /// Runs a monte carlo simulation so its not guaranteed to place all of the points.
    /// </summary>
    /// <param name="numPoints">The number of points to place</param>
    /// <param name="maxIter">The failsafe iterations to stop at</param>
    /// <param name="distributionMap">The density map for the points</param>
    /// <returns>An enumerable set of points</returns>
    public static IEnumerable<Vector2> PlacePoints(int numPoints, int maxIter, Texture2D distributionMap)
    {
        var (min, max) = distributionMap.GetMinMax();

        for (int iterCount = 0, i = 0; i < numPoints && iterCount < maxIter; iterCount++)
        {
            float randx = Random.Range(0, distributionMap.width);
            float randy = Random.Range(0, distributionMap.height);

            float cutoff = 1 - distributionMap.GetPixel((int)randx, (int)randy).grayscale;
            cutoff = Mathf.Clamp(cutoff, min, max);
            float decide = Random.Range(min, max);

            if (decide >= cutoff)
            {
                float permx = Random.Range(0,distributionMap.width/100);
                float permy = Random.Range(0,distributionMap.height/100);
                yield return new Vector2(randx, randy) + new Vector2(permx, permy);
                i++;
            }
        }
    }
}
