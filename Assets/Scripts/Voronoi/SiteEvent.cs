using System.Collections.Generic;
using UnityEngine;
using RobustOrientation;

public class SiteEvent : VoronoiEvent
{
    //I dont know why, but I completely forgot to use m_ naming everywhere else... 
    public Vector2 m_site;
    public Face m_face;

    public SiteEvent(Vector2 site, Face face){
        m_site = site;
        m_face = face;
    }

    //steps 1-5 of HandleSiteEvent in Computational Geometry (p.158)
    public override void HandleEvent(AVLTree beachLine, Utils.PriorityQueue<VoronoiEvent, (double y, double x)> sweepLine, DCEL diagram){
        //steps 1-3 in HandleSiteEvent through InsertArc
        //1 - insert m_site into empty tree if it is empty
        //2 - search for arc A vertically above m_site, set circle event to false alarm
        //3 - replace A with subtree of 2 breakpoints and 3 arcs, then rebalance
        //4 - Create new half-edges in diagram for edges between V(p_i) & V(p_j)
        ArcNode insertedArc = beachLine.InsertArc(m_site, m_face, (double)m_site.y);
        ArcNode closeNeighbor = beachLine.GetLeftNeighbor(insertedArc);
        if(closeNeighbor == null) {return;} //arc is at root and there are no edges yet
        //otherwise, closeNeighbor is the arc that got split
        HalfEdge halfEdge = diagram.CreateHalfEdge(m_face);
        HalfEdge twinEdge = diagram.CreateHalfEdge(closeNeighbor.face);
        halfEdge.twin = twinEdge;
        twinEdge.twin = halfEdge;

        BreakpointNode breakpointNode = beachLine.GetLeftBreakpointNode(insertedArc);
        breakpointNode.halfEdge = halfEdge;

        breakpointNode = beachLine.GetRightBreakpointNode(insertedArc);
        if(breakpointNode != null){breakpointNode.halfEdge = twinEdge;}
        //special case, m_site is level with close neighbor, and there is only one breakpoint between them
        else{beachLine.AddDetachedBreakpoint(new BreakpointNode(m_site, closeNeighbor.site){halfEdge = twinEdge});}

        //5 Check for new triples where m_site acts as the left and right arc of the triple
        //check farNeighbor, closeNeighbor, insertedArc beachline triple
        ArcNode farNeighbor = beachLine.GetLeftNeighbor(closeNeighbor);
        
        //farNeighor cant be insertedArc OR closeNeighbor, and insertedArc cannot be closeNeighbor
        //always a triple unless we have no farNeighbor in one direction
        if(farNeighbor != null) 
        {
            //see if breakpoints converge, p_i, p_j, p_k occur on clockwise turns
            //avoid determinant test, as it is too erronous with floating points
            //instead use a robust version (Shewchuck did the hard work for us :3)

            double det = RO.orient2d(farNeighbor.site, closeNeighbor.site, m_site);
            //use JUST the sign to determine orientation
            //negative = clockwise, positive = counterclockwise, 0 = collinear
            if (det < 0.0)
            {
                //breackpoints converge
                CircleEvent validCircle = new CircleEvent(farNeighbor, closeNeighbor, insertedArc); 
                closeNeighbor.circle = validCircle;
                sweepLine.Enqueue(validCircle, (validCircle.bottomY, validCircle.centerX));
            }
        }
    
        //check insertedArc, closeNeighbor, farNeighbor beachline triple
        closeNeighbor = beachLine.GetRightNeighbor(insertedArc);
        if(closeNeighbor == null) {return;} //special case early return
        farNeighbor = beachLine.GetRightNeighbor(closeNeighbor);
        if(farNeighbor != null) 
        {
            double det = RO.orient2d(m_site, closeNeighbor.site, farNeighbor.site);
            if (det < 0.0)
            {
                //breackpoints converge
                CircleEvent validCircle = new CircleEvent(insertedArc, closeNeighbor, farNeighbor); 
                closeNeighbor.circle = validCircle;
                sweepLine.Enqueue(validCircle, (validCircle.bottomY, validCircle.centerX));
            }
        }
    }
}
