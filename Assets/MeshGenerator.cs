using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using EasyButtons;

public class MeshGenerator : MonoBehaviour
{
    // Start is called before the first frame update

    private Mesh _mesh;

    private Vector3[] _vertices;
    private int[] _triangles;

    public int resolution = 2000;
    public int xWidth = 20;
    public int zWidth = 20;

    public int yScaling = 20;

    public Texture2D heightmap;
    public Texture2D densitymap;

    void Start()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;

        CreateShape();
        UpdateMesh();

    }

    // Update is called once per frame
    void Update()
    {

    }


    [Button]
    public void Regenerate()
    {
        Random.InitState(1);
        CreateShape();
        var r = Delaunay.Generate(new List<Vector3>(_vertices));
        Debug.Log("Done!");
    }

    private IEnumerable<Vector2> PlacePoints(int points, int maxIter)
    {
        var (min, max) = densitymap.GetMinMax();

        for (int iterCount = 0, i = 0; i < points && iterCount < maxIter; iterCount++)
        {
            float randx = Random.Range(0, densitymap.width);
            float randy = Random.Range(0, densitymap.height);

            float cutoff = 1 - densitymap.GetPixel((int)randx, (int)randy).grayscale;
            cutoff = Mathf.Clamp(cutoff, min, max);
            float decide = Random.Range(min, max);

            if (decide >= cutoff)
            {
                yield return new Vector2(randx, randy);
                i++;
                //Debug.Log($"Added {i}, {decide}, {cutoff} points");
            }
            else
            {
                //Debug.Log($"Skipped {i}, {decide}, {cutoff}");
            }
        }
    }

    void CreateShape()
    {
        Assert.IsNotNull(heightmap);
        Assert.IsNotNull(densitymap);
        Assert.AreEqual(heightmap.width, densitymap.width);
        Assert.AreEqual(heightmap.height, densitymap.height);

        _vertices = new Vector3[resolution];

        foreach (var (i, point) in PlacePoints(resolution, 100000).WithIndex())
        {
            float yValue = heightmap.GetPixel((int)point.x, (int)point.y).grayscale * yScaling;

            float x = (point.x / (float)heightmap.width) * xWidth;
            float z = (point.y / (float)heightmap.height) * zWidth;

            _vertices[i] = new Vector3(x, yValue, z);
        }
    }

    void UpdateMesh()
    {

    }

    private void OnDrawGizmos()
    {
        if (_vertices == null)
            return;

        foreach (var v in _vertices)
        {
            Gizmos.DrawSphere(v, .04f);
        }
    }
}

public static class Extensions
{
    public static IEnumerable<(int index, T item)> WithIndex<T>(this IEnumerable<T> self)
    {
        int i = 0;
        foreach (T item in self)
        {
            yield return (i, item);
            i++;
        }
    }
}
