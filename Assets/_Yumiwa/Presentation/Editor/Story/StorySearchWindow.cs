using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Yumiwa.Infrastructure.Story;

namespace Yumiwa.Presentation.Editor.Story
{
    public class StorySearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private StoryGraphView _graphView;
        private EditorWindow _window;

        public void Init(StoryGraphView graphView, EditorWindow window)
        {
            _graphView = graphView;
            _window = window;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
                new SearchTreeGroupEntry(new GUIContent("Nodes"), 1),

                new SearchTreeEntry(new GUIContent("Start Event"))
                {
                    userData = typeof(StartNodeData), level = 2
                },
                new SearchTreeEntry(new GUIContent("End Event"))
                {
                    userData = typeof(EndNodeData), level = 2
                },
                new SearchTreeEntry(new GUIContent("Log Event"))
                {
                    userData = typeof(LogNodeData), level = 2
                },
                new SearchTreeEntry(new GUIContent("Wait Event"))
                {
                    userData = typeof(WaitNodeData), level = 2
                }
            };
            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            var windowMousePosition = _window.rootVisualElement.ChangeCoordinatesTo(_window.rootVisualElement.parent, context.screenMousePosition - _window.position.position);
            var graphMousePosition = _graphView.contentViewContainer.WorldToLocal(windowMousePosition);

            _graphView.CreateNode(SearchTreeEntry.name, (Type)SearchTreeEntry.userData, graphMousePosition);
            return true;
        }
    }
}
