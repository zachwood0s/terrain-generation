using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public int seed = 1234;

    public Texture2D heightmap;
    public Texture2D densitymap;

    void Start()
    {
        Regenerate();
    }

    // Update is called once per frame
    void Update()
    {

    }


    [Button]
    public void Regenerate()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;

        Random.InitState(seed);
        CreateShape();
        UpdateMesh();
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
        var verts2d = new Vector2[resolution];
        Dictionary<Vector2, int> lookup = new Dictionary<Vector2, int>();

        foreach (var (i, point) in PlacePoints(resolution, 100000).WithIndex())
        {
            float yValue = heightmap.GetPixel((int)point.x, (int)point.y).grayscale * yScaling;

            float x = (point.x / (float)heightmap.width) * xWidth;
            float z = (point.y / (float)heightmap.height) * zWidth;

            _vertices[i] = new Vector3(x, yValue, z);
            verts2d[i] = point;
            lookup[point] = i;
        }

        var r = Delaunay.Generate(new List<Vector2>(verts2d));
        _triangles = new int[r.Triangles.Count * 3];

        int vert = 0;
        foreach(var t in r.Triangles.Cast<Delaunay.Triangle>())
        {

            Vector2 a = t.A.V, b = t.B.V, c = t.C.V;
            int aidx = lookup[a], bidx = lookup[b], cidx = lookup[c];
            Vector3 a3 = _vertices[aidx], b3 = _vertices[bidx], c3 = _vertices[cidx];
            Vector3 normal = Vector3.Cross(b3 - a3, c3 - a3).normalized;

            if (normal.y < 0)
            {
                (bidx, cidx) = (cidx, bidx);
                Debug.Log("flipped!");
            }
                
            _triangles[vert + 0] = aidx;
            _triangles[vert + 1] = bidx;
            _triangles[vert + 2] = cidx;
            vert += 3;
        }

    }

    void UpdateMesh()
    {
        _mesh.Clear();

        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;

        _mesh.RecalculateNormals();
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
