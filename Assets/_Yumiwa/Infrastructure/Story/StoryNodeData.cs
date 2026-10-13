using System;
using UnityEngine;

namespace Yumiwa.Infrastructure.Story
{
    [Serializable]
    public abstract class StoryNodeData
    {
        public string NodeId;
        public Vector2 Position;
    }

    [Serializable]
    public class StartNodeData : StoryNodeData
    {
    }

    [Serializable]
    public class EndNodeData : StoryNodeData
    {
    }

    [Serializable]
    public class LogNodeData : StoryNodeData
    {
        public string LogMessage;
    }

    [Serializable]
    public class WaitNodeData : StoryNodeData
    {
        public float WaitSeconds;
    }
}
