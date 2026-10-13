using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Yumiwa.Infrastructure.Story;

namespace Yumiwa.Presentation.Editor.Story
{
    public class StoryGraphView : GraphView
    {
        private StoryEventEditorWindow _window;
        private StorySearchWindow _searchWindow;

        public StoryGraphView(StoryEventEditorWindow window)
        {
            _window = window;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            AddSearchWindow();
        }

        private void AddSearchWindow()
        {
            _searchWindow = ScriptableObject.CreateInstance<StorySearchWindow>();
            _searchWindow.Init(this, _window);
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });

            return compatiblePorts;
        }

        public StoryNode CreateNode(string nodeName, Type nodeType, Vector2 position, string guid = null)
        {
            var node = new StoryNode(nodeName, nodeType);
            if (!string.IsNullOrEmpty(guid))
            {
                node.GUID = guid;
            }
            node.SetPosition(new Rect(position, Vector2.zero));
            AddElement(node);
            return node;
        }

        public void SaveToAsset(StoryGraphAsset asset)
        {
            if (asset == null) return;

            asset.ClearData();

            var nodes = this.nodes.ToList().Cast<StoryNode>().ToList();

            foreach (var node in nodes)
            {
                StoryNodeData data = (StoryNodeData)Activator.CreateInstance(node.DataType);
                data.NodeId = node.GUID;
                data.Position = node.GetPosition().position;

                if (node.DataType == typeof(LogNodeData))
                {
                    ((LogNodeData)data).LogMessage = node.LogMessageField.value;
                }
                else if (node.DataType == typeof(WaitNodeData))
                {
                    ((WaitNodeData)data).WaitSeconds = node.WaitSecondsField.value;
                }

                asset.Nodes.Add(data);
            }

            var edges = this.edges.ToList();
            foreach (var edge in edges)
            {
                var outputNode = edge.output.node as StoryNode;
                var inputNode = edge.input.node as StoryNode;

                asset.Edges.Add(new StoryEdgeData
                {
                    OutputNodeId = outputNode.GUID,
                    InputNodeId = inputNode.GUID
                });
            }
        }

        public void LoadFromAsset(StoryGraphAsset asset)
        {
            DeleteElements(nodes.ToList());
            DeleteElements(edges.ToList());

            if (asset == null) return;

            var nodeDict = new Dictionary<string, StoryNode>();

            foreach (var nodeData in asset.Nodes)
            {
                string nodeName = nodeData.GetType().Name.Replace("NodeData", " Event");
                var node = CreateNode(nodeName, nodeData.GetType(), nodeData.Position, nodeData.NodeId);

                if (nodeData is LogNodeData logData)
                {
                    node.LogMessageField.value = logData.LogMessage;
                }
                else if (nodeData is WaitNodeData waitData)
                {
                    node.WaitSecondsField.value = waitData.WaitSeconds;
                }

                nodeDict.Add(nodeData.NodeId, node);
            }

            foreach (var edgeData in asset.Edges)
            {
                if (nodeDict.TryGetValue(edgeData.OutputNodeId, out var outputNode) &&
                    nodeDict.TryGetValue(edgeData.InputNodeId, out var inputNode))
                {
                    var outputPort = outputNode.outputContainer.Q<Port>();
                    var inputPort = inputNode.inputContainer.Q<Port>();

                    var edge = outputPort.ConnectTo(inputPort);
                    AddElement(edge);
                }
            }
        }

        public void ValidateGraph()
        {
            var nodes = this.nodes.ToList().Cast<StoryNode>().ToList();
            var startNode = nodes.FirstOrDefault(n => n.DataType == typeof(StartNodeData));

            foreach (var node in nodes)
            {
                node.style.backgroundColor = new StyleColor(StyleKeyword.Null); // Reset color
            }

            if (startNode == null)
            {
                Debug.LogError("[StoryGraph] Validation Failed: No Start Node found.");
                return;
            }

            var reachableNodes = new HashSet<StoryNode>();
            var queue = new Queue<StoryNode>();
            queue.Enqueue(startNode);
            reachableNodes.Add(startNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current.DataType == typeof(WaitNodeData) && current.WaitSecondsField.value < 0)
                {
                    Debug.LogWarning($"[StoryGraph] Node {current.title} has invalid wait time.");
                    current.style.backgroundColor = Color.red;
                }

                var edgesOut = edges.ToList().Where(e => e.output.node == current).ToList();
                foreach (var edge in edgesOut)
                {
                    var targetNode = edge.input.node as StoryNode;
                    if (targetNode != null && !reachableNodes.Contains(targetNode))
                    {
                        reachableNodes.Add(targetNode);
                        queue.Enqueue(targetNode);
                    }
                }
            }

            foreach (var node in nodes)
            {
                if (!reachableNodes.Contains(node))
                {
                    Debug.LogWarning($"[StoryGraph] Node {node.title} is unreachable.");
                    node.style.backgroundColor = Color.red;
                }
            }

            Debug.Log("[StoryGraph] Validation complete.");
        }
    }
}
