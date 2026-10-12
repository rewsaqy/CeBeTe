using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Yumiwa.Infrastructure.Story;

namespace Yumiwa.Presentation.Editor.Story
{
    public class StoryNode : Node
    {
        public string GUID;
        public Type DataType;

        // Data fields mapped to UI elements
        public TextField LogMessageField;
        public FloatField WaitSecondsField;

        public StoryNode(string nodeName, Type dataType)
        {
            title = nodeName;
            GUID = Guid.NewGuid().ToString();
            DataType = dataType;

            GeneratePorts();
            GenerateDataFields();
        }

        private void GeneratePorts()
        {
            if (DataType != typeof(StartNodeData))
            {
                var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
                inputPort.portName = "Input";
                inputContainer.Add(inputPort);
            }

            if (DataType != typeof(EndNodeData))
            {
                var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
                outputPort.portName = "Output";
                outputContainer.Add(outputPort);
            }
        }

        private void GenerateDataFields()
        {
            if (DataType == typeof(LogNodeData))
            {
                LogMessageField = new TextField("Log Message");
                extensionContainer.Add(LogMessageField);
            }
            else if (DataType == typeof(WaitNodeData))
            {
                WaitSecondsField = new FloatField("Wait Seconds");
                extensionContainer.Add(WaitSecondsField);
            }

            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
