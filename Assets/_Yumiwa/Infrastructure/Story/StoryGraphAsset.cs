using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yumiwa.Infrastructure.Story
{
    [Serializable]
    public struct StoryEdgeData
    {
        public string OutputNodeId;
        public string InputNodeId;
    }

    [CreateAssetMenu(menuName = "Yumiwa/Story/Story Graph", fileName = "NewStoryGraph")]
    public class StoryGraphAsset : ScriptableObject
    {
        [SerializeReference]
        public List<StoryNodeData> Nodes = new List<StoryNodeData>();

        public List<StoryEdgeData> Edges = new List<StoryEdgeData>();

        public void ClearData()
        {
            Nodes.Clear();
            Edges.Clear();
        }
    }
}
