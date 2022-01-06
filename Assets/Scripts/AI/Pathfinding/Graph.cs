using System.Linq;
using System.Collections.Generic;

using UnityEngine;

public class Graph : MonoBehaviour
{
    public List<GraphNode> nodes = new List<GraphNode>();

	public int FindNearestNode(Vector3 position)
    {
		GraphNode tempNearestNode = null;
		float tempMinimumDistance = Mathf.Infinity;
		
		foreach(GraphNode tempNode in nodes)
        {
			float tempDistance = (tempNode.transform.position - position).magnitude;
			if(tempDistance < tempMinimumDistance)
            {
				tempNearestNode = tempNode;
				tempMinimumDistance = tempDistance;
            }
        }
		return tempNearestNode.nodeIndex;
    }

	public List<int> FindPath(int startPosition, int endPosition)
    {
		bool tempTargetNodeFound = false;

		float tempDistanceCost;

		List<int> tempRoute = new List<int>();
		List<float> tempCosts = new List<float>();

		List<GraphEdge> tempTraversedEdges = new List<GraphEdge>();

        for (int i = 0; i < nodes.Count; i++)
        {
			tempRoute.Add(i);
			tempCosts.Add(Mathf.Infinity);
        }

		PriorityQueue<GraphEdge> tempEdgePriorityQueue = new PriorityQueue<GraphEdge>(true);

		tempCosts[startPosition] = 0f;
		foreach (GraphEdge _edge in nodes[startPosition].adjacencyList)
		{
			tempEdgePriorityQueue.Enqueue(_edge.travelCost, _edge);
		}

		while(tempEdgePriorityQueue.Count > 0)
        {
			GraphEdge tempCurrentEdge = tempEdgePriorityQueue.Dequeue();
			tempTraversedEdges.Add(tempCurrentEdge);

			if(tempCosts[tempCurrentEdge.toNodeIndex] > tempCosts[tempCurrentEdge.fromNodeIndex] + tempCurrentEdge.travelCost)
            {
				tempDistanceCost =
					Mathf.Abs(nodes[tempCurrentEdge.fromNodeIndex].transform.position.x - nodes[tempCurrentEdge.toNodeIndex].transform.position.x) +
					Mathf.Abs(nodes[tempCurrentEdge.fromNodeIndex].transform.position.z - nodes[tempCurrentEdge.toNodeIndex].transform.position.z);

				tempRoute[tempCurrentEdge.toNodeIndex] = tempCurrentEdge.fromNodeIndex;
				tempCosts[tempCurrentEdge.toNodeIndex] = tempCosts[tempCurrentEdge.fromNodeIndex] + tempCurrentEdge.travelCost + tempDistanceCost;
				if(tempCurrentEdge.toNodeIndex == endPosition)
                {
					tempTargetNodeFound = true;
				}
            }

			foreach (GraphEdge _edge in nodes[tempCurrentEdge.toNodeIndex].adjacencyList)
			{
				if (!tempTraversedEdges.Contains(_edge) && !tempEdgePriorityQueue.Contains(_edge))
				{
					tempEdgePriorityQueue.Enqueue(_edge.travelCost, _edge);
				}
			}
		}

		if (tempTargetNodeFound)
        {
			List<int> tempPath = new List<int>();
			int tempCurrentNode = endPosition;
			tempPath.Add(tempCurrentNode);
			while (tempCurrentNode != startPosition)
			{
				tempCurrentNode = tempRoute[tempCurrentNode];
				tempPath.Add(tempCurrentNode);
			}
			return tempPath;
		}

		return new List<int>();
    }
}
