
using UnityEngine;

//ArcNodes are stores at the leaves of the BST
public class ArcNode : INode
{
    public INode Left { get; set; }         //always NULL
    public INode Right { get; set; }        //always NULL
    public INode Parent { get; set; }       //always BreakpointNode or NULL (empty tree)
    public int Height { get; set; } = 0;    //always 0

    //site that is generating this arc and face of that site
    public Vector2 site;
    public Face face;
    //circle event that is going to make this site dissapear
    public CircleEvent circle;

    public ArcNode(Vector2 site, Face face)
    {
        this.site = site;
        this.face = face;
    }

}