using System.Linq;
using System.Threading;
using UnityEngine;
using Yumiwa.Infrastructure.Story;

namespace Yumiwa.Presentation.Story
{
    public class StoryRunner : MonoBehaviour
    {
        [SerializeField] private StoryGraphAsset _storyGraph;

        private CancellationTokenSource _cts;

        public async Awaitable RunStoryAsync(StoryGraphAsset graphOverride = null)
        {
            var graph = graphOverride != null ? graphOverride : _storyGraph;
            if (graph == null || graph.Nodes.Count == 0)
            {
                Debug.LogWarning("[StoryRunner] Cannot run empty or null graph.");
                return;
            }

            var startNode = graph.Nodes.FirstOrDefault(n => n is StartNodeData);
            if (startNode == null)
            {
                Debug.LogError("[StoryRunner] Graph has no Start node.");
                return;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var token = _cts.Token;
            var currentNodeId = startNode.NodeId;

            while (!string.IsNullOrEmpty(currentNodeId))
            {
                if (token.IsCancellationRequested) break;

                var currentNode = graph.Nodes.FirstOrDefault(n => n.NodeId == currentNodeId);
                if (currentNode == null) break;

                await ExecuteNodeAsync(currentNode, token);

                if (currentNode is EndNodeData)
                {
                    break;
                }

                var outgoingEdge = graph.Edges.FirstOrDefault(e => e.OutputNodeId == currentNodeId);
                currentNodeId = outgoingEdge.InputNodeId;
            }
        }

        private async Awaitable ExecuteNodeAsync(StoryNodeData node, CancellationToken token)
        {
            switch (node)
            {
                case StartNodeData _:
                    Debug.Log("[StoryRunner] Story Started.");
                    break;

                case EndNodeData _:
                    Debug.Log("[StoryRunner] Story Ended.");
                    break;

                case LogNodeData logNode:
                    Debug.Log($"[StoryRunner] Log: {logNode.LogMessage}");
                    break;

                case WaitNodeData waitNode:
                    Debug.Log($"[StoryRunner] Waiting for {waitNode.WaitSeconds} seconds...");
                    await Awaitable.WaitForSecondsAsync(waitNode.WaitSeconds, cancellationToken: token);
                    break;
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
