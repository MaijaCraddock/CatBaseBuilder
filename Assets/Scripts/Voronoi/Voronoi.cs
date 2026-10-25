using System.Collections.Generic;
using UnityEngine;

public class Voronoi
{

    public DCEL GenerateVoronoiDiagram(List<Vector2> voronoiSites, Vector2? maxXYParam = null, Vector2? minXYParam = null){
        //NOTE: for now, the maxXY and minXY dont do much, sense the min and max extend in bounding anyways.
        //If I get more time to work on this project, I intend to add an optional extend bool that culls points and verts outside the bounds without extension.
        if (voronoiSites.Count == 0){return null;}
        
        //set bounding box to param or nothing (immediately set to max/min of site's x/y when bounding)
        Vector2 maxXY = maxXYParam ?? new Vector2(-float.MaxValue, -float.MaxValue);
        Vector2 minXY = minXYParam ?? new Vector2(float.MaxValue, float.MaxValue);

        //priority queue is a max heap, highest priority item is item with the largest y-coordinate
        Comparer<(double y, double x)> maxHeapComparer = Comparer<(double y, double x)>.Create((a, b) => {
            int cmp = b.y.CompareTo(a.y);                   //ret larger y-coord
            return (cmp != 0) ? cmp : a.x.CompareTo(b.x);   //tie break with lower x-coord
        }); 

        //Max Heap - event with the largest y - coordinate is popped 
        Utils.PriorityQueue<VoronoiEvent, (double y, double x)> sweepLine = new Utils.PriorityQueue<VoronoiEvent, (double y, double x)>(maxHeapComparer);

        //binary search tree: internal : stores breakpoint of arc | leaf : stores arc
        AVLTree beachLine = new AVLTree();

        //stores our final voronoi diagram once complete
        DCEL diagram = new DCEL(maxXY, minXY);

        //usually we sort the sites before adding to queue
        //but priority queue would sort by the same method anyways, so its redundant
        //insert all site events into sweepLine

        //small note: we will likely use some quasi-randomness for voronoiSites generation
        //so unique sites are guaranteed without a seen/hashset mechanism
        foreach (Vector2 site in voronoiSites)
        {
            //each face is defined by a site     
            Face face = diagram.CreateFace(site);
            //O(nlogn), but not worth speeding up, circleEvents will take the most work!
            sweepLine.Enqueue(new SiteEvent(site, face), (site.y, site.x));
        }

        //move through the sweepline until it is empty.
        //when the sweepline moves to the next Event,
        //it is visualized as having jumped down to that event's y-coordinate.
        while(sweepLine.Count > 0){
            VoronoiEvent ev = sweepLine.Dequeue(); //peeks and returns maxHeap comparer event in queue (next event)

            //CircleEvent and SiteEvent overload this function
            ev.HandleEvent(beachLine, sweepLine, diagram);
            
        }

        diagram.BoundBoxInfiniteHalfEdges(beachLine.GetAllBreakpointNodes());

        return diagram;
    }
}
