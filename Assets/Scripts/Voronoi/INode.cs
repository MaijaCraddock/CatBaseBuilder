public interface INode
{
    INode Left { get; set; }
    INode Right { get; set; }
    INode Parent { get; set; }
    int Height { get; set; }
}