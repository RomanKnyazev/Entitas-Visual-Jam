﻿using System;
using System.IO;
using Entitas.Visual.Model.VO;
using PureMVC.Patterns.Proxy;
using UnityEngine;

namespace Entitas.Visual.Model
{
    public class GraphProxy : Proxy
    {
        public const string NodeAdded = "GraphNodeAdded";
        public const string NodeRemoved = "GraphNodeRemoved";

        public const string GraphPath = "/Graph.json";

        public const string Name = "GraphProxy";
        private string _totalGraphPath;

        public GraphProxy() : base(Name)
        {
            Graph graph;

            _totalGraphPath = Application.dataPath + GraphPath;

            if (File.Exists(_totalGraphPath))
            {
                graph = JsonUtility.FromJson<Graph>(File.ReadAllText(_totalGraphPath));
            }
            else
            {
                graph = new Graph();
                SaveGraph(graph);
            }

            Data = graph;
        }

        public void AddNewNode(Vector2 mousePosition)
        {
            var newNode = new Node
            {
                Position = new Rect(mousePosition, new Vector2(240f, 80f))
            };
            GraphData.Nodes.Add(newNode);
            SaveGraph(GraphData);

            SendNotification(NodeAdded, newNode);
        }

        public void RemoveNode(Node node)
        {
            var removedNode = GraphData.Nodes.Remove(node);
            SaveGraph(GraphData);

            SendNotification(NodeRemoved, removedNode);
        }

        public void UpdateNodePosition(Node node, Vector2 newPosition)
        {
            node.Position.position = newPosition;
            SaveGraph(GraphData);
        }

        public void CollapseNode(Node node)
        {
            node.IsCollapsed = !node.IsCollapsed;
            SaveGraph(GraphData);
        }

        public void AddFieldToNode(Node node, Type type)
        {
            node.Fields.Add(new Field("newField", type.FullName));
            SaveGraph(GraphData);
        }

        public Graph GraphData
        {
            get { return (Graph) Data; }
        }

        private void SaveGraph(Graph graph)
        {
            var serializedString = JsonUtility.ToJson(graph);
            File.WriteAllText(_totalGraphPath, serializedString);
        }
    }
}