using UnityEngine;
using System;
using System.Collections.Generic;
using RobustOrientation;

public class CircleEvent : VoronoiEvent
{
    public bool valid = true;
    public ArcNode leaf = null;
    public double centerX;
    private double centerY;
    public double bottomY;

    //set leaf, centerX, centerY and bottomY variables
    //call orientation2d before creating a CircleEvent!
    //leaf - pointer to leaf in Binary Search Tree that will be removed on event
    //centerX/Y - x,y coordinates of center of circle, which is used to create a vertex in the diagram
    //bottomY - bottom of the circle, used for event priority
    public CircleEvent(ArcNode a, ArcNode b, ArcNode c)
    {
        double acx, acy, bcx, bcy;
        double a2, b2;
        double ux, uy;
        double radius;

        leaf = b;

        Vector2 pa = a.site;
        Vector2 pb = b.site;
        Vector2 pc = c.site;

        acx = (double)pa.x - pc.x;
        bcx = (double)pb.x - pc.x;
        acy = (double)pa.y - pc.y;
        bcy = (double)pb.y - pc.y;
        
        double D  = 2.0 * (acx * bcy - acy * bcx);
        
        //if the sign of D is not the same as the sign of the robust orientation check
        //we instead calculate a robust determinant (sites are near collinear)
        if(D >= 0){
            //Note: I think there is some avoidable waste with the method I use, which involves
            //calculating orient2d and then again orient2d_exact_determinant if non-robust version
            //is not satisfactory. But right now its best to keep moving! Maybe I will revisit later
            D = 2 * RO.orient2d_exact_determinant(pa, pb, pc);
        }

        a2 = acx * acx + acy * acy;
        b2 = bcx * bcx + bcy * bcy;

        ux = (bcy * a2 - acy * b2) / D;
        uy = (acx * b2 - bcx * a2) / D;

        radius = Math.Sqrt(ux * ux + uy * uy);
        
        centerX = pc.x + ux;
        centerY = pc.y + uy;

        //bottomY = centerY - radius; -- apparently the following loses precision on large circles!
        //the following is more accurate
        double dy = (uy > 0.0)
            ? -(ux * ux) / (uy + radius)    //radius^2 - uy^2 = ux^2
            : uy - radius;                  //uy <= 0

        bottomY = pc.y + dy;
    }

    public override void HandleEvent(AVLTree beachLine, Utils.PriorityQueue<VoronoiEvent, (double y, double x)> sweepLine, DCEL diagram){
        if (!valid){return;}
        
        //1 - Remove leaf that is being swallowed by circle event, and all circle events that use leaf
        ArcNode leftNeighbor = beachLine.GetLeftNeighbor(leaf);
        if (leftNeighbor.circle != null){
            leftNeighbor.circle.valid = false;
            leftNeighbor.circle = null;
        }
        ArcNode rightNeighbor = beachLine.GetRightNeighbor(leaf);
        if (rightNeighbor.circle != null){
            rightNeighbor.circle.valid = false;
            rightNeighbor.circle = null;
        }
        //read parts needed for step 2 before leaf is removed
        HalfEdge ijEdge = beachLine.GetLeftBreakpointNode(leaf).halfEdge;    //face = p_j
        HalfEdge jkEdge = beachLine.GetRightBreakpointNode(leaf).halfEdge;    //face = p_k
        //2 - Add center of circle to DCEL diagram as vertex, set half edges for breakpoints
        //new half edges
        HalfEdge ikHalfEdge = diagram.CreateHalfEdge(rightNeighbor.face);
        HalfEdge ikTwinEdge = diagram.CreateHalfEdge(leftNeighbor.face);
        ikHalfEdge.twin = ikTwinEdge;
        ikTwinEdge.twin = ikHalfEdge;
        BreakpointNode ikBreakpoint = beachLine.RemoveArc(leaf);
        ikBreakpoint.halfEdge = ikHalfEdge;
        Vertex vertex = diagram.CreateVertex(centerX, centerY);
        //old edges end at vertex, new edge begins at vertex
        ijEdge.twin.origin = vertex;
        jkEdge.twin.origin = vertex;
        ikHalfEdge.origin = vertex;
        //face p_j
        ijEdge.next = jkEdge.twin;
        jkEdge.twin.prev = ijEdge;
        //face p_k
        jkEdge.next = ikHalfEdge;
        ikHalfEdge.prev = jkEdge;
        //face p_i  
        ikTwinEdge.next = ijEdge.twin;
        ijEdge.twin.prev = ikTwinEdge;
        vertex.incidentEdge = ikHalfEdge;
        //3 - Check for triples p_i, leftNeighbor, rightNeighbor and leftNeighbor, rightNeighbor, p_k
        ArcNode otherNeighbor = beachLine.GetLeftNeighbor(leftNeighbor);
        //very similar code to SiteEvent, check for comments on logic
        if(otherNeighbor != null && otherNeighbor != rightNeighbor){
            //p_i, leftNeighbor, rightNeighbor
            double det = RO.orient2d(otherNeighbor.site, leftNeighbor.site, rightNeighbor.site);
            if (det < 0.0)
            {
                CircleEvent validCircle = new CircleEvent(otherNeighbor, leftNeighbor, rightNeighbor); 
                leftNeighbor.circle = validCircle;
                sweepLine.Enqueue(validCircle, (validCircle.bottomY, validCircle.centerX));
            }
        }
        otherNeighbor = beachLine.GetRightNeighbor(rightNeighbor);
        if(otherNeighbor != null && otherNeighbor != leftNeighbor){
            //leftNeighbor, rightNeighbor, p_k
            double det = RO.orient2d(leftNeighbor.site, rightNeighbor.site, otherNeighbor.site);
            if (det < 0.0)
            {
                CircleEvent validCircle = new CircleEvent(leftNeighbor, rightNeighbor, otherNeighbor); 
                rightNeighbor.circle = validCircle;
                sweepLine.Enqueue(validCircle, (validCircle.bottomY, validCircle.centerX));
            }
        }
    }
}
