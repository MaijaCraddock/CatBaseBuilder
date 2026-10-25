using System.Collections.Generic;
using UnityEngine;

public abstract class VoronoiEvent
{
    public abstract void HandleEvent(AVLTree beachLine, Utils.PriorityQueue<VoronoiEvent, (double y, double x)> sweepLine, DCEL diagram);
}
