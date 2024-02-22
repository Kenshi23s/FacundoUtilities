using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor;

public class Node : MonoBehaviour
{
    [SerializeField]
    private List<Node> _neighbors = new();

    public int cost = 1;


    private void Awake()
    {
        if (TryGetComponent<Collider>(out var col))
        {
            Destroy(col);
        }
    }

#if UNITY_EDITOR

    [ContextMenu("MakeConnection")]
    void MakeConnection()
    {
        var col = Selection.gameObjects.Select(x => x.GetComponent<Node>()).Where(x => x != null).Where(x => x != this).ToArray();
        if (!col.Any()) return;

        foreach (var item in col)
        {
            item.AddNode(FList.Create(this));

        }
        AddNode(col);
    }


    [ContextMenu("BreakConnection")]
    void BreakConnection()
    {
        var col = Selection.gameObjects.Select(x => x.GetComponent<Node>()).Where(x => x != null).Where(x => x != this).ToArray();
        if (!col.Any()) return;

        foreach (var item in col)
        {
            item.AddNode(FList.Create(this));

        }
        AddNode(col);
    }




#endif


    internal void AddNode(IEnumerable<Node> nodes)
    {
        _neighbors = _neighbors.Concat(nodes).Distinct().ToList();
    }

    internal void RemoveNodes(IEnumerable<Node> nodes)
    {
        _neighbors = _neighbors.Where(x => !nodes.Contains(x)).ToList();
    }

    public void ClearNullNodes()
    {
        _neighbors = _neighbors.Where(x => x != null).ToList();
    }


    private void OnValidate()
    {
        ClearNullNodes();
    }

    public List<Node> GetNeighbors()
    {
        var col = _neighbors.Where(x => x != null).ToList();
        if (col == null || col == default)
        {
            col = new();
        }
        return col;
    }


    public bool CheckNeighbor(Node cNode)
    {
        foreach (Node item in _neighbors)
        {
            if (item == cNode) return true;
        }

        return false;
    }

    public void AddNeighbor(Node nodeToAdd)
    {
        _neighbors.Add(nodeToAdd);
    }

    public bool HasNeighbours()
    {
        return _neighbors.Count > 0;

    }

    public void ClearNeighbours()
    {
        _neighbors.Clear();
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
       
        foreach (var item in _neighbors.Where(x => x != null))
        {
            foreach (var item2 in item.GetNeighbors().Where(x => x != null))
            {
                if (item2 == this)
                {
                    Gizmos.DrawLine(transform.position, item.transform.position);
                }

            }
            //me tira error
        }
    }
}
