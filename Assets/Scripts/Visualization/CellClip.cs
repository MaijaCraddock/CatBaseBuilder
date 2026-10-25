using System.Collections.Generic;
using UnityEngine;

// Helpers for working with the cells of a finished diagram.
//
// The diagram's bounding box grows to include every Voronoi vertex, and the vertices of cells on the outside of
// the diagram can be extremely far away (often many times the area the sites occupy, sometimes thousands of times).
// Anything that draws or places things should clip each cell to the area it actually cares about first.
public static class CellClip
{
    // counter clockwise corners of a cell, walked along the half-edges. null if the walk does not close
    public static List<Vector2> Boundary(Face face)
    {
        if (face.incidentEdge == null) return null;
        var poly = new List<Vector2>();
        HalfEdge start = face.incidentEdge, e = start;
        int guard = 1000000;
        do { poly.Add(e.origin.position); e = e.next; } while (e != start && --guard > 0);
        return guard > 0 ? poly : null;
    }

    // cells are convex, so clipping one to a rectangle gives a convex polygon, or nothing if the cell is outside it.
    // done in doubles, because the far away corners are large numbers
    public static List<Vector2> ClipToRect(List<Vector2> poly, Vector2 min, Vector2 max)
    {
        if (poly == null || poly.Count < 3) return null;

        var cur = new List<Pt>(poly.Count);
        foreach (Vector2 v in poly) cur.Add(new Pt(v.x, v.y));

        cur = Clip(cur, 0, min.x, true);     // x >= min.x
        cur = Clip(cur, 0, max.x, false);    // x <= max.x
        cur = Clip(cur, 1, min.y, true);     // y >= min.y
        cur = Clip(cur, 1, max.y, false);    // y <= max.y

        // drop the duplicate points made where a corner sits exactly on a clip line
        var result = new List<Vector2>(cur.Count);
        for (int i = 0; i < cur.Count; i++)
        {
            Pt p = cur[i];
            if (result.Count > 0 && Near(result[result.Count - 1], p)) continue;
            result.Add(new Vector2((float)p.x, (float)p.y));
        }
        if (result.Count > 1 && Near(result[0], cur[cur.Count - 1])) result.RemoveAt(result.Count - 1);
        return result.Count >= 3 ? result : null;
    }

    // average of the corners, which is always inside a convex polygon. a safe apex for a triangle fan
    public static Vector2 Center(List<Vector2> poly)
    {
        double x = 0, y = 0;
        foreach (Vector2 v in poly) { x += v.x; y += v.y; }
        return new Vector2((float)(x / poly.Count), (float)(y / poly.Count));
    }

    struct Pt { public double x, y; public Pt(double x, double y) { this.x = x; this.y = y; } }

    static bool Near(Vector2 a, Pt b) { return System.Math.Abs(a.x - b.x) < 1e-6 && System.Math.Abs(a.y - b.y) < 1e-6; }

    // keep the part of the polygon where coordinate (x if axis 0, y if axis 1) is above / below the bound
    static List<Pt> Clip(List<Pt> input, int axis, double bound, bool keepAbove)
    {
        var output = new List<Pt>(input.Count + 2);
        int n = input.Count;
        for (int i = 0; i < n; i++)
        {
            Pt a = input[i], b = input[(i + 1) % n];
            double da = Signed(a, axis, bound, keepAbove), db = Signed(b, axis, bound, keepAbove);
            if (da >= 0) output.Add(a);
            if ((da >= 0) != (db >= 0))
            {
                double t = da / (da - db);
                output.Add(new Pt(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t));
            }
        }
        return output;
    }

    static double Signed(Pt p, int axis, double bound, bool keepAbove)
    {
        double c = axis == 0 ? p.x : p.y;
        return keepAbove ? c - bound : bound - c;
    }
}
