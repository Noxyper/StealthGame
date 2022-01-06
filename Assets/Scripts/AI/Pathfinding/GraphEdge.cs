using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GraphEdge
{
    public int toNodeIndex;
    public int fromNodeIndex;
    [Space]
    public float travelCost;

    public GraphEdge(int from, int to, float cost)
    {
        toNodeIndex = to;
        fromNodeIndex = from;
            
        travelCost = cost;
    }
}
