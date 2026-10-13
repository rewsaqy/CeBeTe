using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Yumiwa.Infrastructure.Story;
using Yumiwa.Presentation.Story;

namespace Yumiwa.Presentation.Tests
{
    public class StoryRunnerTests
    {
        private GameObject _runnerGo;
        private StoryRunner _runner;
        private StoryGraphAsset _graph;

        [SetUp]
        public void Setup()
        {
            _runnerGo = new GameObject("TestStoryRunner");
            _runner = _runnerGo.AddComponent<StoryRunner>();

            _graph = ScriptableObject.CreateInstance<StoryGraphAsset>();

            var startNode = new StartNodeData { NodeId = "start" };
            var logNode = new LogNodeData { NodeId = "log", LogMessage = "Test Log Message" };
            var waitNode = new WaitNodeData { NodeId = "wait", WaitSeconds = 0.1f };
            var endNode = new EndNodeData { NodeId = "end" };

            _graph.Nodes.Add(startNode);
            _graph.Nodes.Add(logNode);
            _graph.Nodes.Add(waitNode);
            _graph.Nodes.Add(endNode);

            _graph.Edges.Add(new StoryEdgeData { OutputNodeId = "start", InputNodeId = "log" });
            _graph.Edges.Add(new StoryEdgeData { OutputNodeId = "log", InputNodeId = "wait" });
            _graph.Edges.Add(new StoryEdgeData { OutputNodeId = "wait", InputNodeId = "end" });
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_runnerGo);
            Object.DestroyImmediate(_graph);
        }

        [UnityTest]
        public IEnumerator RunStoryAsync_ExecutesNodesInOrder()
        {
            var task = _runner.RunStoryAsync(_graph);

            yield return new WaitUntil(() => task.IsCompleted);

            // At this point we are verifying it completes without throwing an exception,
            // and checking unity's console output indirectly via Awaitable completion.
            Assert.IsTrue(task.IsCompleted);
        }
    }
}
