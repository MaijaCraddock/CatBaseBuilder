using System.Collections.Generic;
using UnityEngine;

// Sits on every cell object. It carries the cell's data, so anything attached to the same object
// (a noise component, interaction, spawning) can read it without going back to the diagram.
public class VoronoiCell : MonoBehaviour
{
    public int cellId;
    public Vector2 site;                 // the site, in diagram space. This object's origin sits on it
    public uint seed;                    // stable per-cell seed, for this cell's own noise
    public Color color;
    public Vector2[] corners;            // the cell's corners relative to this object, counter clockwise
                                         // (clipped to the play area)

    [System.NonSerialized] public Face face;   // the diagram face, for adjacency (walk incidentEdge / twin)

    MaterialPropertyBlock block;

    public void Init(int cellId, Face face, Vector2 site, List<Vector2> corners, Color color, uint seed)
    {
        this.cellId = cellId;
        this.face = face;
        this.site = site;
        this.corners = corners.ToArray();
        this.seed = seed;
        SetColor(color);
    }

    // every cell shares one material, the color is a per-renderer override
    public void SetColor(Color c)
    {
        if(WorldNoise.Instance == null){Debug.Log("FYUCK");}
        Biome biome = WorldNoise.Instance.GetBiomeHeight(site);
        color = biome.color;
        if (block == null) block = new MaterialPropertyBlock();
        block.SetColor("_Color", color);
        GetComponent<MeshRenderer>().SetPropertyBlock(block);
    }
}
