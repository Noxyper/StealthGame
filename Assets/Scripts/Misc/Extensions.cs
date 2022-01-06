using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public static class ExtensionMethods
{
    public static List<float> GetVectorAsList(this Vector3 vector)
    {
        List<float> tempList = new List<float>();

        tempList.Add(vector.x);
        tempList.Add(vector.y);
        tempList.Add(vector.z);

        return tempList;
    }

    public static T MostOf<T>(this ICollection<T> sequence)
    {
        return (from i in sequence
                group i by i into g
                orderby g.Count() descending
                select g.Key).First();
    }
}

// Taken from https://www.dotnetlovers.com/article/231/priority-queue
public class PriorityQueue<T>
{
    class Node
    {
        public float Priority { get; set; }
        public T Object { get; set; }
    }

    List<Node> _queue = new List<Node>();
    int _heapSize = -1;
    bool _isMinPriorityQueue;

    public int Count { get { return _queue.Count; } }

    public PriorityQueue(bool isMinPriorityQueue = false)
    {
        _isMinPriorityQueue = isMinPriorityQueue;
    }

    public void Enqueue(float priority, T obj) 
    {
        Node node = new Node() { Priority = priority, Object = obj };
        _queue.Add(node);
        _heapSize++;
        if (_isMinPriorityQueue)
            BuildHeapMin(_heapSize);
        else
            BuildHeapMax(_heapSize);
    }
    
    public T Dequeue() 
    {
        if (_heapSize > -1)
        {
            var returnVal = _queue[0].Object;
            _queue[0] = _queue[_heapSize];
            _queue.RemoveAt(_heapSize);
            _heapSize--;

            if (_isMinPriorityQueue)
                MinHeapify(0);
            else
                MaxHeapify(0);

            return returnVal;
        }
        else
            throw new System.Exception("The Priority Queue is Empty!");
    }
    
    public void UpdatePriority(T obj, float priority) 
    {
        for (int i = 0; i < _heapSize; i++)
        {
            Node node = _queue[i];
            if (ReferenceEquals(node.Object, obj))
            {
                node.Priority = priority;
                if(_isMinPriorityQueue)
                {
                    BuildHeapMin(i);
                    MinHeapify(i);
                }
                else
                {
                    BuildHeapMax(i);
                    MaxHeapify(i);
                }
            }
        }
    }

    public bool Contains(T obj) 
    {
        foreach (Node node in _queue)
            if (ReferenceEquals(node.Object, obj))
                return true;
        return false;
    }

    private void BuildHeapMax(int i) 
    {
        while(i >= 0 && _queue[(i - 1) / 2].Priority < _queue[i].Priority)
        {
            Swap(i, (i - 1) / 2);
            i = (i - 1) / 2;
        }
    }
    private void BuildHeapMin(int i) 
    {
        while (i >= 0 && _queue[(i - 1) / 2].Priority > _queue[i].Priority)
        {
            Swap(i, (i - 1) / 2);
            i = (i - 1) / 2;
        }
    }
    private void MaxHeapify(int i) 
    {
        int left = ChildL(i);
        int right = ChildR(i);

        int highest = i;

        if (left <= _heapSize && _queue[highest].Priority < _queue[left].Priority)
            highest = left;
        
        if (right <= _heapSize && _queue[highest].Priority < _queue[right].Priority)
            highest = right;
     
        if(highest != i)
        {
            Swap(highest, i);
            MaxHeapify(highest);
        }
    }
    private void MinHeapify(int i) 
    {
        int left = ChildL(i);
        int right = ChildR(i);

        int lowest = i;

        if (left <= _heapSize && _queue[lowest].Priority > _queue[left].Priority)
            lowest = left;

        if (right <= _heapSize && _queue[lowest].Priority > _queue[right].Priority)
            lowest = right;

        if (lowest != i)
        {
            Swap(lowest, i);
            MinHeapify(lowest);
        }
    }

    private void Swap(int i, int j)
    {
        var temp = _queue[i];
        _queue[i] = _queue[j];
        _queue[j] = temp;
    }
    private int ChildL(int i)
    {
        return i * 2 + 1;
    }
    private int ChildR(int i)
    {
        return i * 2 + 2;
    }
}