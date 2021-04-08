using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
    private Color[] _colors;

    private float _minHeight;
    private float _maxHeight;


    [Header("Terrain Settings")]
    public int resolution = 2000;
    public int xWidth = 20;
    public int zWidth = 20;

    public float yScaling = 20;
    public int seed = 1234;

    public Texture2D heightmap;
    public Texture2D densitymap;

    public Gradient gradient;


    [Header("Triangulation Settings")]
    public float minAngle = 0.0f;
    public float maxAngle = 0.0f;


    void Start()
    {
        Regenerate();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMesh();
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

    void CreateShape()
    {
        Assert.IsNotNull(heightmap);
        Assert.IsNotNull(densitymap);
        Assert.AreEqual(heightmap.width, densitymap.width);
        Assert.AreEqual(heightmap.height, densitymap.height);

        _vertices = new Vector3[resolution];
        var verts2d = new Vector2[resolution];
        Dictionary<Vector2, int> lookup = new Dictionary<Vector2, int>();

        foreach (var (i, point) in PointGeneration.PlacePoints(resolution, 100000, densitymap).WithIndex())
        {
            float yValue = heightmap.GetPixel((int)point.x, (int)point.y).grayscale * yScaling;

            float x = (point.x / (float)heightmap.width) * xWidth;
            float z = (point.y / (float)heightmap.height) * zWidth;

            _vertices[i] = new Vector3(x, yValue, z);
            verts2d[i] = new Vector2(x, z);
            lookup[verts2d[i]] = i;
        }

        WritePointsToFile(verts2d);

        var r = Delaunay.Generate(verts2d.ToList());
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
            }
                
            _triangles[vert + 0] = aidx;
            _triangles[vert + 1] = bidx;
            _triangles[vert + 2] = cidx;
            vert += 3;
        }

        _maxHeight = _vertices.Max(x => x.y);
        _minHeight = _vertices.Min(x => x.y);

        _colors = new Color[verts2d.Length];
    }

    void WritePointsToFile(Vector2[] points)
    {
        var lines = points.Select(x => $"{x.x} {x.y}");
        lines = lines.Prepend(points.Length.ToString());
        File.WriteAllLines("Assets/points.txt", lines);
    }

    void UpdateMesh()
    {
        for (int i = 0; i < _vertices.Length; i++)
        {
            float height = Mathf.InverseLerp(_minHeight, _maxHeight, _vertices[i].y);
            _colors[i] = gradient.Evaluate(height);
        }

        _mesh.Clear();

        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
        _mesh.colors = _colors;

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
