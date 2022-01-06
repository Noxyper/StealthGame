using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphNode : MonoBehaviour
{
    public int nodeIndex;
    public List<GraphEdge> adjacencyList = new List<GraphEdge>();
}
