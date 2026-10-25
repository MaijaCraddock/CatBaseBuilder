using System;
using System.Collections.Generic;
using UnityEngine;

//we use an AVL tree to keep tree balanced, because we use height to determine rotations,
//we can insure that arc nodes always remain leaves.
//Also a Google search said it requires less comparisons, and the less we CalculateBreakpoint the better

public class AVLTree
{   
    private INode root;
    private List<BreakpointNode> detachedBreakpoints = new List<BreakpointNode>();
    public void AddDetachedBreakpoint(BreakpointNode breakpoint){detachedBreakpoints.Add(breakpoint);}
    //return -1 if node is null, or height if node exists
    private int HeightOf(INode node) => node == null ? -1 : node.Height;

    //height of node is longest path to leaf
    //called recursively/iteratively from leaf up
    private void UpdateHeight(INode node)
    {
        node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));
    }

    //<= -2 ===> right subtree is too tall, >= 2 ===> left subtree is too tall
    private int BalanceFactor(INode node)
    {
        return HeightOf(node.Left) - HeightOf(node.Right);
    }

    //swap levels of node and its right child, maintain level of grandchild node
    private INode RotateLeft(INode node)
    {                                                       //before rotate     after rotate
        INode rightChild = node.Right;                      //      p               p
        INode leftGrandchild = rightChild.Left;             //      |               |
                                                            //      n               rC
        rightChild.Left = node;                             //        \            /
        node.Right = leftGrandchild;                        //          rC   =>   n
                                                            //         /           \ 
        rightChild.Parent = node.Parent;                    //       lGC            lGC
        node.Parent = rightChild;                           
                                                                    
        //leftGrandchild always non-null
        //otherwise it means we rotated an arc to internal node
        leftGrandchild.Parent = node;

        UpdateHeight(node);
        UpdateHeight(rightChild);

        return rightChild;
    }
 
    private INode RotateRight(INode node)
    {                                                       //before rotate     after rotate
        INode leftChild = node.Left;                        //      p               p
        INode rightGrandChild = leftChild.Right;            //      |               |
                                                            //      n               lC
        leftChild.Right = node;                             //    /                   \
        node.Left = rightGrandChild;                        //   lC        =>          n
                                                            //     \                  /
        leftChild.Parent = node.Parent;                     //      rGC             rGC
        node.Parent = leftChild;
        
        //rightGrandchild always non-null
        //otherwise it means we rotated an arc to internal node
        rightGrandChild.Parent = node;
 
        UpdateHeight(node);
        UpdateHeight(leftChild);
 
        return leftChild;
    }

    private INode Rebalance(INode node)
    {
        UpdateHeight(node);
        int balanceFactor = BalanceFactor(node);

        if(balanceFactor >= 2)    //left side too heavy
        {
            if(BalanceFactor (node.Left) < 0)
            {                                 
                //left-right requires 2 rotations      
                node.Left = RotateLeft(node.Left);      //makes it a straight line to rotate
            }
            return RotateRight(node);
        }

        if(balanceFactor <= -2)   //right side too heavy
        {
            if(BalanceFactor (node.Right) > 0)
            {                                 
                //right-left requires 2 rotations      
                node.Right = RotateRight(node.Right);   //makes it a straight line to rotate
            }
            return RotateLeft(node);
        }

        return node;
    }

    //path node up until we hit root, rebalance on each ancestor 
    //called each time insert/delete happens
    private void RebalancePathFromNode(INode node){
        while(node != null)
        {
            //subroot of our subtree is the possible rebalance of our subtree
            //if no rebalance needed, subRoot == node
            //if left heavy,  subRoot == leftChild rotated up a level  (or rightChild of leftChild if left-right)
            //if right heavy, subRoot == rightChild rotated up a level (or leftChild of rightChild if right-left)
            INode subRoot = Rebalance(node);
            INode parent = subRoot.Parent;

            //if parent is null, we are the new root, and we're done
            if(parent == null){
                root = subRoot;
            }


            //else become child of previous nodes parent (in its place)
            else if(parent.Left == node){
                parent.Left = subRoot;
            }

            else{
                parent.Right = subRoot;
            }

            subRoot.Parent = parent;
            node = parent; //sets to null and breaks loop on first if statement
        }
    }

    //arguments are the x-coordinate of an arc and the location of our sweepline
    //find the arc directly above a new arc in a site event
    public ArcNode FindArcAbove(double x, double directrix)
    {
        //root != null already checked prior to function
        INode currNode = root;
        while(currNode is BreakpointNode bpNode){
            //Calculate the breakpoint, then traverse the tree to the left or right of the breakpoint's x coordinate
            //repeated until we reach an arc / leaf node which tells us exactly which arc we are under
            currNode = (x < bpNode.CalculateBreakpoint(directrix)) ? bpNode.Left : bpNode.Right;
        }
        return (ArcNode)currNode;
    }

    //GetRight/Left Neighbors is needed for triple detection
    public ArcNode GetLeftNeighbor(ArcNode node)
    {
        INode currNode = node;
        //while node is not the root, and we are the left child of a node, traverse upwards
        while (currNode.Parent != null && currNode.Parent.Left == currNode){currNode = currNode.Parent;}

        if(currNode.Parent == null){return null;} //leftmost child (no arcs to the left)

        //previous node is the .Right, traverse to the left node once
        currNode = currNode.Parent.Left;
        //until we reach a new arc, move towards the rightmost arc
        while(currNode is BreakpointNode){currNode = currNode.Right;}
        return (ArcNode)currNode;
    }

    public ArcNode GetRightNeighbor(ArcNode node)
    {
        //same logic as getting left neighbor but directionally flipped
        INode currNode = node;
        while (currNode.Parent != null && currNode.Parent.Right == currNode){currNode = currNode.Parent;}

        if(currNode.Parent == null){return null;} //rightmost child (no arcs to the right)

        currNode = currNode.Parent.Right;
        while(currNode is BreakpointNode){currNode = currNode.Left;}
        return (ArcNode)currNode;
    }

    //used during deletion of an arc and during site events
    //return of null on a site event means there is not half edge to be created yet
    public BreakpointNode GetLeftBreakpointNode(ArcNode node)
    {
        INode currNode = node;
        while(currNode.Parent != null)
        {
            if(currNode.Parent.Right == currNode){return (BreakpointNode)currNode.Parent;}
            currNode = currNode.Parent;
        }
        return null;
    }

    public BreakpointNode GetRightBreakpointNode(ArcNode node)
    {
        INode currNode = node;
        while(currNode.Parent != null)
        {
            if(currNode.Parent.Left == currNode){return (BreakpointNode)currNode.Parent;}
            currNode = currNode.Parent;
        }
        return null;
    }

    

    //                                       insertion of p_j splits one arc p_i into 2 breakpoints and 3 arcs
    //                                                                                                  P    (parentNode)
    //                                                                                                  |
    //                 (parentNode)     P                                                         BP(p_i, p_j)    (leftBreakpoint)
    //                                  |                       ===>                           /               \
    //                  (arcAbove)   Arc(p_i)                                   (leftArc)  Arc(p_i)           BP(p_j, p_i)    (rightBreakpoint)
    //                                                                                                           /         \
    //                                                                                        (middleArc)  Arc( p_j)         Arc(p_i)   (rightArc)
    //Note: in Computational Geometry this p_j arc is labeled as p_i, I prefer p_i, p_j, p_i beachline labels
    //returns ArcNode to be used in SiteEvent's event handling
    public ArcNode InsertArc(Vector2 p_j, Face face_j, double directrix)
    {
        //first special case, where there is not an arc in the beachline yet
        if(root == null)
        {
            ArcNode rootArc = new ArcNode(p_j, face_j);
            root = rootArc;
            return rootArc;
        }

        ArcNode arcAbove = FindArcAbove(p_j.x, directrix);
        if(arcAbove.circle != null){arcAbove.circle.valid = false; arcAbove.circle = null;}
        INode parentNode = arcAbove.Parent;
        Vector2 p_i = arcAbove.site;
        Face face_i = arcAbove.face;

        //second special case, where arcAbove is really from a collinear site (and there is only place where arc splits)
        if(arcAbove.site.y == p_j.y)
        {
            ArcNode levelArc = new ArcNode(p_j, face_j);
            BreakpointNode levelBreakpoint = new BreakpointNode(p_i, p_j)
            {
                Left = arcAbove,
                Right = levelArc
            };
            arcAbove.Parent = levelBreakpoint;
            levelArc.Parent = levelBreakpoint;
            UpdateHeight(levelBreakpoint);

            levelBreakpoint.Parent = parentNode;
            if(parentNode == null){root = levelBreakpoint;}
            else if(parentNode.Left == arcAbove){parentNode.Left = levelBreakpoint;}
            else{parentNode.Right = levelBreakpoint;}
            RebalancePathFromNode(levelBreakpoint);

            return levelArc;
        }

        //regular case, arc is split in two places by new arc
        //new arc splits arc directly above it into two new arcs
        ArcNode leftArc = new ArcNode(p_i, face_i);
        ArcNode middleArc = new ArcNode(p_j, face_j);
        ArcNode rightArc = new ArcNode(p_i, face_i);

        //set node to the parent + child fields (depicted in the beautiful ascii art above function)
        BreakpointNode rightBreakpoint = new BreakpointNode(p_j, p_i)
        {
            Left = middleArc,
            Right = rightArc
        };
        middleArc.Parent = rightBreakpoint;
        rightArc.Parent = rightBreakpoint;
        UpdateHeight(rightBreakpoint);

        BreakpointNode leftBreakpoint = new BreakpointNode(p_i, p_j)
        {  
            Left = leftArc,
            Right = rightBreakpoint 
        };
        leftArc.Parent = leftBreakpoint;
        rightBreakpoint.Parent = leftBreakpoint;
        UpdateHeight(leftBreakpoint);

        leftBreakpoint.Parent = parentNode;

        //if null, Arc(p_i) was only arc in tree
        if(parentNode == null){root = leftBreakpoint;}
        else if (parentNode.Left == arcAbove){parentNode.Left = leftBreakpoint;}
        else if (parentNode.Right == arcAbove){parentNode.Right = leftBreakpoint;}
        RebalancePathFromNode(leftBreakpoint); //only need to rebalance from parentNode, but it could be null
        
        return middleArc;
    }

    //for consecutive leaves p_i, p_j, p_k, at the moment that p_j is "swallowed"
    //remove p_j from the beachline, and breakpoints <p_i, p_j> and <p_j, p_k>
    //becomes single breakpoint <p_i, p_k>, then check for new triples.
    
    //the sibling of p_j replaces the lower of the <p_i, p_j> | <p_j, p_k>
    //the higher of <p_i, p_j> | <p_j, p_k> is replaced with <p_i, p_k>

    //                      (Not a totally accurate representation, read notes to the side)
    //          BP(p_i, p_j)                 BP(p_j, p_k)                         BP(p_i, p_k)              -- BP(p_i, p_k) replaces whichever of BP(p_i, p_j)
    //           /      \                     /      \                              /       \                  or BP(p_j, p_k) was closer to the root.
    //        ...         P                 P        ...            =>            ...      ... 
    //        |           |                |           |                          |           |             -- additionally, ONE of BP(p_i, p_j) or BP(p_j, p_k)
    //  Arc(p_i)        Arc(p_j)     Arc(p_j)        Arc(p_k)                 Arc(p_i)     Arc(p_k)            will ALWAYS be P


    //we also delete circle events where p_j is to the left and to the right
    //the circle event where p_j is in the middle is being processed when this is being called
    //this is not handled in AVLTree.
    public BreakpointNode RemoveArc(ArcNode p_j)
    {
        ArcNode p_i = GetLeftNeighbor(p_j);
        ArcNode p_k = GetRightNeighbor(p_j);

        BreakpointNode leftBreakpoint = GetLeftBreakpointNode(p_j);
        BreakpointNode rightBreakpoint = GetRightBreakpointNode(p_j);
        BreakpointNode parentNode = (BreakpointNode)p_j.Parent;
        
        //sibling can be p_i, p_k or a subtree which leaves in p_i or p_k
        INode sibling = (parentNode.Left == p_j) ? parentNode.Right : parentNode.Left;
        INode grandparentNode = parentNode.Parent;
        sibling.Parent = grandparentNode;

        //sibling replaces parent (lower of <p_i, p_j> | <p_j, p_k>)
        if(grandparentNode == null){root = sibling;}
        else if(grandparentNode.Left == parentNode){grandparentNode.Left = sibling;}
        else{grandparentNode.Right = sibling;}

        //whichever of <p_i, p_j> | <p_j, p_k> is closer to the root becomes <p_i, p_k>
        if(parentNode == leftBreakpoint){rightBreakpoint.breakpoint = (p_i.site, p_k.site);}
        else{leftBreakpoint.breakpoint = (p_i.site, p_k.site);} //parentNode == rightBreakpoint

        RebalancePathFromNode(sibling);

        //return <p_i, p_k> for CircleEvent
        return (parentNode == leftBreakpoint) ? rightBreakpoint : leftBreakpoint;
    }


    //used to close all infinite half edges defined by breakpoints on the beachline when all events are handled
    //these breakpoints are bound by some bounding box in DCEL
    public List<BreakpointNode> GetAllBreakpointNodes()
    {
        List<BreakpointNode> breakpointList = new List<BreakpointNode>();
        //DFS on the tree, put every breakpoint into the list
        Stack<INode> breakpointStack = new Stack<INode>();
        breakpointStack.Push(root); //impossible to not have some leftover breakpoints
        while(breakpointStack.Count > 0)
        {
            if(breakpointStack.Pop() is BreakpointNode breakpoint)
            {
                breakpointList.Add(breakpoint);
                breakpointStack.Push(breakpoint.Left);
                breakpointStack.Push(breakpoint.Right);
            }
        }
        
        breakpointList.AddRange(detachedBreakpoints);
        return breakpointList;
    }
}

