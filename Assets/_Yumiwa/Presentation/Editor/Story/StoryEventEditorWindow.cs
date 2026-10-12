using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Yumiwa.Infrastructure.Story;

namespace Yumiwa.Presentation.Editor.Story
{
    public class StoryEventEditorWindow : EditorWindow
    {
        private StoryGraphView _graphView;
        private StoryGraphAsset _currentGraph;

        [MenuItem("Yumiwa/Story Event Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<StoryEventEditorWindow>("Story Event Editor");
            window.titleContent = new GUIContent("Story Event Editor");
        }

        public static void OpenWindowWithAsset(StoryGraphAsset asset)
        {
            var window = GetWindow<StoryEventEditorWindow>("Story Event Editor");
            window.titleContent = new GUIContent("Story Event Editor");
            window.LoadGraph(asset);
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
        }

        private void ConstructGraphView()
        {
            _graphView = new StoryGraphView(this)
            {
                name = "Story Graph"
            };

            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            var saveButton = new Button(() => SaveGraph()) { text = "Save Graph" };
            toolbar.Add(saveButton);

            var loadButton = new Button(() => RequestLoadGraph()) { text = "Load Graph" };
            toolbar.Add(loadButton);

            var validateButton = new Button(() => _graphView.ValidateGraph()) { text = "Validate" };
            toolbar.Add(validateButton);

            rootVisualElement.Add(toolbar);
        }

        private void SaveGraph()
        {
            if (_currentGraph == null)
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Story Graph", "NewStoryGraph", "asset", "Save Story Graph");
                if (string.IsNullOrEmpty(path)) return;

                _currentGraph = ScriptableObject.CreateInstance<StoryGraphAsset>();
                AssetDatabase.CreateAsset(_currentGraph, path);
            }

            _graphView.SaveToAsset(_currentGraph);
            EditorUtility.SetDirty(_currentGraph);
            AssetDatabase.SaveAssets();
            Debug.Log($"[StoryEditor] Saved to {_currentGraph.name}");
        }

        private void RequestLoadGraph()
        {
            string path = EditorUtility.OpenFilePanel("Load Story Graph", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;

            path = FileUtil.GetProjectRelativePath(path);
            var asset = AssetDatabase.LoadAssetAtPath<StoryGraphAsset>(path);
            if (asset != null)
            {
                LoadGraph(asset);
            }
        }

        private void LoadGraph(StoryGraphAsset asset)
        {
            _currentGraph = asset;
            _graphView.LoadFromAsset(asset);
            Debug.Log($"[StoryEditor] Loaded {_currentGraph.name}");
        }
    }
}
