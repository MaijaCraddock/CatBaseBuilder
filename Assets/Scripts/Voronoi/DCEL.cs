using UnityEngine;
using System.Collections.Generic;

//circle event creates these
public class Vertex
{
    public HalfEdge incidentEdge;
    public Vector2 position;

    public Vertex(double x, double y)
    {
        position = new Vector2((float)x, (float)y);
    }
}

//face is a site creating it
//and a half edge (which can be traversed to find the full perimeter)
public class Face
{
    //our faces do not have inner components, only need 1 edge to define
    //site in face
    public readonly Vector2 site;
    public HalfEdge incidentEdge;

    public Face(Vector2 site)
    {
        this.site = site;
    }
}

//half edge goes from origin -> next.origin
//twin goes from next.origin -> origin
public class HalfEdge
{
    public Vertex origin;
    public HalfEdge twin; //edge going the opposite direction INTO src on another face
    public HalfEdge prev;
    public HalfEdge next;
    public Face incidentFace; //face on left (edges go counterclockwise)

    public HalfEdge(Face incidentFace)
    {
        this.incidentFace = incidentFace;
    }
}

public class DCEL
{
    private Vector2 maxXY; //max X and Y value for bounding box
    private Vector2 minXY; //min X and Y value for bounding box
    public List<Vertex> vertexList = new List<Vertex>();
    public List<HalfEdge> halfEdgeList = new List<HalfEdge>();
    public List<Face> faceList = new List<Face>();
    private List<HalfEdge> perimeterEdges = new List<HalfEdge>(); //used to construct null face half edges after bounding box
    public DCEL(Vector2 maxXY, Vector2 minXY)
    {
        this.maxXY = maxXY;
        this.minXY = minXY;
    }
    
    public Vertex CreateVertex(double x, double y)
    {
        Vertex vertex = new Vertex(x, y);
        vertexList.Add(vertex);
        return vertex;
    }

    //call this twice, and set twin of each afterwards
    public HalfEdge CreateHalfEdge(Face incidentFace)
    {
        HalfEdge halfEdge = new HalfEdge(incidentFace);
        halfEdgeList.Add(halfEdge);
        if(incidentFace != null && incidentFace.incidentEdge == null) {incidentFace.incidentEdge = halfEdge;}
        return halfEdge;
    }

    public Face CreateFace(Vector2 site)
    {
        Face face = new Face(site);
        faceList.Add(face);
        return face;
    }

    //extends bounding box to encompass all sites and vertices
    private void ExtendBoundingBox()
    {
        bool extended = false;
        foreach(Face face in faceList)
        {
            if(face.site.x > maxXY.x){maxXY.x = face.site.x; extended = true;}
            if(face.site.y > maxXY.y){maxXY.y = face.site.y; extended = true;}
            if(face.site.x < minXY.x){minXY.x = face.site.x; extended = true;}
            if(face.site.y < minXY.y){minXY.y = face.site.y; extended = true;}
        }

        foreach(Vertex vertex in vertexList)
        {
            if(vertex.position.x > maxXY.x){maxXY.x = vertex.position.x; extended = true;}
            if(vertex.position.y > maxXY.y){maxXY.y = vertex.position.y; extended = true;}
            if(vertex.position.x < minXY.x){minXY.x = vertex.position.x; extended = true;}
            if(vertex.position.y < minXY.y){minXY.y = vertex.position.y; extended = true;}
        }

        if(extended){
            //add some padding so that there are no sites/vertices on the edge, if possible
            double diffX = (0.1*maxXY.x) - (0.1*minXY.x);
            double diffY = (0.1*maxXY.y) - (0.1*minXY.y);
            //if diffs are 0, pad to at least a small amount
            if(diffX == 0){diffX = 1;} 
            if(diffY == 0){diffY = 1;}
            maxXY.x = (float)((double)maxXY.x + diffX);
            maxXY.y = (float)((double)maxXY.y + diffY);
            minXY.x = (float)((double)minXY.x - diffX);
            minXY.y = (float)((double)minXY.y - diffY);
        }
    }

    //link two vertices with a new half edge pair
    //all twin edges will have null face, as they lie on the perimeter
    private Face perimeterFace;
    private void LinkPerimeterVertexPair(Vertex vi, Vertex vj)
    {
        if(vi.incidentEdge != null){perimeterFace = vi.incidentEdge.twin.incidentFace;}

        HalfEdge halfEdge = CreateHalfEdge(perimeterFace);
        HalfEdge twinEdge = CreateHalfEdge(null); //outer face is the perimeter
        
        halfEdge.origin = vi;
        halfEdge.twin = twinEdge;

        twinEdge.origin = vj;
        twinEdge.twin = halfEdge;

        if(vi.incidentEdge != null)
        {
            halfEdge.prev = vi.incidentEdge.twin;
            vi.incidentEdge.twin.next = halfEdge;
        }

        //this perimeter edge leads into the leaving diagram edge
        if(vj.incidentEdge != null)
        {
            halfEdge.next = vj.incidentEdge;
            vj.incidentEdge.prev = halfEdge;
        }

        perimeterEdges.Add(halfEdge);
    }

    public void BoundBoxInfiniteHalfEdges(List<BreakpointNode> breakpointList)
    {
        //keep track of where edge 1 leaves
        const int BOTTOM = 0;
        const int RIGHT = 1;
        const int TOP = 2;
        const int LEFT = 3;

        ExtendBoundingBox();
        List<Vertex>[] perimeterCCW = new List<Vertex>[] //starts from bottom left corner
        {
            new List<Vertex>(), //bottom bound, index 0 = bottom left
            new List<Vertex>(), //right bound, index 0 = bottom right
            new List<Vertex>(), //top bound, index 0 = top right
            new List<Vertex>()  //upper bound, index 0 = top left
        };
        
        //add corners to perimeter
        perimeterCCW[BOTTOM].Add(CreateVertex(minXY.x, minXY.y)); 
        perimeterCCW[RIGHT].Add(CreateVertex(maxXY.x, minXY.y));
        perimeterCCW[TOP].Add(CreateVertex(maxXY.x, maxXY.y));
        perimeterCCW[LEFT].Add(CreateVertex(minXY.x, maxXY.y));

        //find where TWIN edge is going, half-edge itself may be set on a vertex already or not
        foreach (BreakpointNode breakpointNode in breakpointList)
        {
            //line from p_j -> p_i that bisects the breakpoint
            double dx = (double)breakpointNode.breakpoint.p_j.x - breakpointNode.breakpoint.p_i.x;
            double dy = (double)breakpointNode.breakpoint.p_j.y - breakpointNode.breakpoint.p_i.y;

            //rotate 90 degrees for dy/dx of half edge being bisected by p_j and p_i
            //clockwise rotation gives proper incidentFace (to left of half edge)
            double temp = dy;
            dy = -dx;
            dx = temp;

            //midpoint between p_i and p_j
            double midX = ((double)breakpointNode.breakpoint.p_i.x + breakpointNode.breakpoint.p_j.x) * 0.5;
            double midY = ((double)breakpointNode.breakpoint.p_i.y + breakpointNode.breakpoint.p_j.y) * 0.5;

            //see how far each midpoint is from left/right (diffX) or up/down (diffY) - infinite if parallel
            double diffX = dx > 0 ? (maxXY.x - midX) / dx : dx < 0 ? (minXY.x - midX) / dx : double.PositiveInfinity;
            double diffY = dy > 0 ? (maxXY.y - midY) / dy : dy < 0 ? (minXY.y - midY) / dy : double.PositiveInfinity;

            Vertex perimeterVertex;
            double boundX, boundY;
            int perimeterSide;
            if (diffX < diffY) //hits a side before it hits a vert bound
            {
                boundX = dx > 0 ? maxXY.x : minXY.x;
                boundY = midY + diffX * dy;
                perimeterSide = dx > 0 ? 1 : 3;

                perimeterVertex = CreateVertex(boundX, boundY);
                perimeterCCW[perimeterSide].Add(perimeterVertex);
            }
            else if (diffX > diffY) //hits a vert bound before a side
            {
                boundX = midX + diffY * dx;
                boundY = dy > 0 ? maxXY.y : minXY.y;
                perimeterSide = dy > 0 ? 2 : 0;

                perimeterVertex = CreateVertex(boundX, boundY);
                perimeterCCW[perimeterSide].Add(perimeterVertex);
            }
            else //tx == ty, hits a corner
            {
                if(dx > 0) //maxXY.x
                {
                    if(dy > 0) //maxXY.y
                    {
                        perimeterVertex = perimeterCCW[TOP][0]; //top right
                    }
                    else //minXY.y
                    {
                        perimeterVertex = perimeterCCW[RIGHT][0]; //bottom right
                    }
                }
                else //dx < 0, minXY.x 
                {
                    if(dy > 0) //maxXY.y
                    {
                        perimeterVertex = perimeterCCW[LEFT][0]; //top left
                    }
                    else //minXY.y
                    {
                        perimeterVertex = perimeterCCW[BOTTOM][0]; //bottom left
                    }
                }
            }

            breakpointNode.halfEdge.twin.origin = perimeterVertex;
            perimeterVertex.incidentEdge = breakpointNode.halfEdge.twin;
        }

        //sort in counter clockwise order, keeping corners in index 0
        perimeterCCW[BOTTOM].Sort((a, b) => a.position.x.CompareTo(b.position.x));
        perimeterCCW[RIGHT].Sort((a, b) => a.position.y.CompareTo(b.position.y));
        perimeterCCW[TOP].Sort((a, b) => b.position.x.CompareTo(a.position.x));   
        perimeterCCW[LEFT].Sort((a, b) => b.position.y.CompareTo(a.position.y));
        
        perimeterFace = faceList[0];
        bool foundExit = false;
        for(int SIDE = LEFT; SIDE >= BOTTOM && !foundExit; SIDE--)
        {
            for(int i = perimeterCCW[SIDE].Count - 1; i >= 0; i--)
            {
                if(perimeterCCW[SIDE][i].incidentEdge != null)
                {
                    perimeterFace = perimeterCCW[SIDE][i].incidentEdge.twin.incidentFace;
                    foundExit = true;
                    break;
                }
            }
        }

        for(int SIDE = BOTTOM; SIDE <= LEFT; SIDE++)
        {
            for(int i = 0; i < perimeterCCW[SIDE].Count - 1; i++)
            {
                LinkPerimeterVertexPair(perimeterCCW[SIDE][i], perimeterCCW[SIDE][i+1]);
            }
            //link last side's vertices to next side's corner
            LinkPerimeterVertexPair(perimeterCCW[SIDE][perimeterCCW[SIDE].Count-1], perimeterCCW[(SIDE+1)%4][0]);
        }

        int perimeterCount = perimeterEdges.Count;
        for(int i = 0; i < perimeterCount; i++)
        {
            HalfEdge current = perimeterEdges[i];
            HalfEdge following = perimeterEdges[(i+1)%perimeterCount];

            //shared vertex is a corner no diagram edge touches, so the face's boundary continues straight along the perimeter
            if(following.origin.incidentEdge == null)
            {
                current.next = following;
                following.prev = current;
            }

            //null face half edges run the opposite way around the perimeter
            following.twin.next = current.twin;
            current.twin.prev = following.twin;
        }

        //corners now get an incidentEdge, after the checks above no longer need them to be null
        foreach(HalfEdge edge in perimeterEdges)
        {
            if(edge.origin.incidentEdge == null){edge.origin.incidentEdge = edge;}
        }
    }
}
