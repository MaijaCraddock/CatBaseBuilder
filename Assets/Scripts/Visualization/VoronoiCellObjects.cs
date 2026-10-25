using System.Collections.Generic;
using UnityEngine;

public class VoronoiCellObjects : MonoBehaviour
{
    [Header("Appearance")]
    public Shader shader;
    public float perimeterWidth = 0.01f;
    public Color perimeterColor = new Color(0.05f, 0.05f, 0.08f, 1f);
    public bool addColliders = true;
    public bool fitCamera = true;

    Material material;
    string status = "";

    void Start() 
    { 
        Generate(); 
    }

    void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.R)) 
        { 
            WorldSettings.Instance.SetSeed(); 
            WorldNoise.Instance.SetOffsets();
            Generate(); 
        }
#endif
    }

    void OnGUI() { GUI.Label(new Rect(10, 10, 700, 24), status); }

    // live tweaking from the inspector while playing
    void OnValidate() { if (material != null) PushMaterialSettings(); }



    [ContextMenu("Regenerate")]
    public void Generate()
    {
        Clear();

        List<Vector2> sites = PoissonDiscSampler.GenerateN(WorldSettings.Instance.worldSize, WorldSettings.Instance.worldSize, WorldSettings.Instance.siteCount);
        // the play area is passed as the minimum bounding box, so even a handful of sites fills the whole area
        DCEL diagram = new Voronoi().GenerateVoronoiDiagram(sites, new Vector2(WorldSettings.Instance.worldSize, WorldSettings.Instance.worldSize), new Vector2(0, 0));

        EnsureMaterial();

        Vector2 areaMin = new Vector2(0, 0), areaMax = new Vector2(WorldSettings.Instance.worldSize, WorldSettings.Instance.worldSize);
        int built = 0;
        for (int id = 0; id < diagram.faceList.Count; id++)
        {
            Face face = diagram.faceList[id];
            // cells on the outside of the diagram can have corners thousands of units away, so clip to the play area
            List<Vector2> poly = CellClip.ClipToRect(CellClip.Boundary(face), areaMin, areaMax);
            if (poly == null) continue;
            CreateCell(id, face, poly);
            built++;
        }

        FitCamera();
        status = $"seed {WorldSettings.Instance.seed} | {built} cell objects | R = new seed";
    }

    void CreateCell(int id, Face face, List<Vector2> poly)
    {
        Vector2 site = face.site;

        var go = new GameObject("Cell " + id, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(site.x, site.y, 0f);

        // corners relative to the site, because the object's origin is the site
        var local = new List<Vector2>(poly.Count);
        foreach (Vector2 corner in poly) local.Add(corner - site);

        Mesh mesh = BuildMesh(local);
        mesh.name = go.name;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;

        var cell = go.AddComponent<VoronoiCell>();
        cell.Init(id, face, site, local, CellColor(id), Hash((uint)WorldSettings.Instance.seed, (uint)id));

        if (addColliders) go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    // a fan of triangles (center, a, b) for every edge a -> b. The cell is convex, so the fan tiles it exactly.
    // Each triangle gets its own 3 vertices because the shader data differs per triangle:
    //   uv.y: 0 at the center, 1 along the cell's edge uv.z: distance from the center to that edge
    // uv.y is linear across the triangle, so (1 - uv.y) * uv.z is the distance from a pixel to the perimeter
    static Mesh BuildMesh(List<Vector2> local)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector3>();
        var tris = new List<int>();

        Vector2 center = CellClip.Center(local);
        int n = local.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = local[i], b = local[(i + 1) % n];
            Vector2 ab = b - a;
            float len = ab.magnitude;
            if (len < 1e-6f) continue;                                      // zero length edge, nothing to draw

            float h = Mathf.Abs(ab.x * (center.y - a.y) - ab.y * (center.x - a.x)) / len;

            int i0 = verts.Count;
            verts.Add(new Vector3(center.x, center.y, 0)); uvs.Add(new Vector3(0, 0, h));
            verts.Add(new Vector3(a.x, a.y, 0));           uvs.Add(new Vector3(0, 1, h));
            verts.Add(new Vector3(b.x, b.y, 0));           uvs.Add(new Vector3(0, 1, h));
            tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // a different color for every cell: hue, saturation and value each step by an irrational amount (the R3
    // low discrepancy sequence), so no two cells share a color and neighbors end up well separated.
    // fractional parts are taken in double so the colors stay distinct for large cell counts
    public static Color CellColor(int id)
    {
        float h = Fraction(0.5 + id * 0.8191725133961645);
        float s = 0.45f + 0.35f * Fraction(0.5 + id * 0.6710436067037893);
        float v = 0.70f + 0.25f * Fraction(0.5 + id * 0.5497004779019703);
        return Color.HSVToRGB(h, s, v);
    }

    static float Fraction(double x) { return (float)(x - System.Math.Floor(x)); }

    static uint Hash(uint a, uint b)
    {
        unchecked
        {
            uint h = (a ^ 0x9E3779B1u) * 0x85EBCA6Bu + b * 0xC2B2AE35u + 0x165667B1u;
            h ^= h >> 15; h *= 0x2C1B3C6Du; h ^= h >> 12; h *= 0x297A2D39u; h ^= h >> 15;
            return h;
        }
    }

    void EnsureMaterial()
    {
        if (material == null)
            material = new Material(shader != null ? shader : Shader.Find("Unlit/VoronoiCell"));
        PushMaterialSettings();
    }

    void PushMaterialSettings()
    {
        material.SetFloat("_EdgeWidth", perimeterWidth);
        material.SetColor("_EdgeColor", perimeterColor);
    }

    void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            MeshFilter filter = child.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null) DestroyAny(filter.sharedMesh);   // meshes are not freed with the object
            DestroyAny(child);
        }
    }

    static void DestroyAny(Object o)
    {
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    void FitCamera()
    {
        Camera cam = Camera.main;
        if (!fitCamera || cam == null || !cam.orthographic) return;

        Vector3 center = transform.TransformPoint(new Vector3(WorldSettings.Instance.worldSize * 0.5f, WorldSettings.Instance.worldSize * 0.5f, 0f));
        cam.transform.position = new Vector3(center.x, center.y, -10f);
        cam.orthographicSize = Mathf.Max(WorldSettings.Instance.worldSize * 0.5f, WorldSettings.Instance.worldSize * 0.5f / cam.aspect) * 1.05f;
    }
}
