using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.EditorCoroutines.Editor;
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
    public Quality quality;

    private bool _started = false;

    EditorCoroutine coroutine;

    public bool animate = false;


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
        if(animate)
        {
            if(coroutine != null)
                EditorCoroutineUtility.StopCoroutine(coroutine);
            Time.timeScale = 1;
            //CreateShape();
            //UpdateMesh();
            coroutine = EditorCoroutineUtility.StartCoroutineOwnerless(InsertPoints());
            Debug.Log("Done!");
        }
        else 
        {
            CreateShape();
            UpdateMesh();
        }
    }

    void CreateShape()
    {
        Assert.IsNotNull(heightmap);
        Assert.IsNotNull(densitymap);
        Assert.AreEqual(heightmap.width, densitymap.width);
        Assert.AreEqual(heightmap.height, densitymap.height);

        /*
        var d = Delaunay.Generate(new List<Vector2>{new Vector2(0.0f, 0.0f), new Vector2(10.0f, 0.0f), new Vector2(5.0f, 5.0f)});
        d.Insert(new Vector2(5.0f, 0.0f), d.Graph.Edges[8]);
        d.Insert(new Vector2(2.0f, 2.0f), d.Graph.Edges[10]);

        d.Finish(false);
        var r = d.Graph;
        */


        var verts2d = PointGeneration.PlacePoints(resolution, 100000, densitymap).ToList();

        var r = Mesher.Triangulate(verts2d, quality);
        UpdateMeshFromDelaunay(r);
    }

    private void UpdateMeshFromDelaunay(Graph r)
    {
        var triangles = r.Triangles.ToList();
        _vertices = new Vector3[triangles.Count * 3];
        _triangles = new int[triangles.Count * 3];

        var tris = r.Triangles.Cast<Delaunay.Triangle>();
        int vert = 0;
        foreach (var (i, tri) in tris.WithIndex())
        {
            Vector3 a = ConvertPoint(tri.A.V), b = ConvertPoint(tri.B.V), c = ConvertPoint(tri.C.V);
            int aind = vert + 0, bind = vert + 1, cind = vert + 2;

            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

            if (normal.y < 0)
            {
                (bind, cind) = (cind, bind);
            }

            _vertices[vert + 0] = a;
            _vertices[vert + 1] = b;
            _vertices[vert + 2] = c;
            _triangles[vert + 0] = aind;
            _triangles[vert + 1] = bind;
            _triangles[vert + 2] = cind;
            vert += 3;
        }

        _maxHeight = _vertices.Max(x => x.y);
        _minHeight = _vertices.Min(x => x.y);

        var verts = _vertices.Select(x => new Vector2(x.x, x.z)).Distinct().ToArray();
        WritePointsToFile(verts);

        Debug.Log($"Created {triangles.Count} triangles and {r.Vertices.Count} vertices");

        _colors = new Color[_vertices.Length];
    }

    public IEnumerator InsertPoints()
    {
        var points = PointGeneration.PlacePoints(resolution, 100000, densitymap).ToList();
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
            if (i != imax)
                del.Insert(pt);
            
            if(i >= 3)
            {
                UpdateMeshFromDelaunay(del._arr);
                UpdateMesh();
            }
            yield return null;
        }
        Debug.Log("Done with base triangulation");
        yield return new EditorWaitForSeconds(10f);
        var q = new QualityCheck(del, quality);
        yield return q.EnforceEnum(() => {UpdateMeshFromDelaunay(del._arr); UpdateMesh();});
        _started = false;
    }

    private Vector3 ConvertPoint(Vector2 point)
    {
        float yValue = heightmap.GetPixel((int)point.x, (int)point.y).grayscale * yScaling;

        float x = (point.x / (float)heightmap.width) * xWidth;
        float z = (point.y / (float)heightmap.height) * zWidth;

        return new Vector3(x, yValue, z);
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
