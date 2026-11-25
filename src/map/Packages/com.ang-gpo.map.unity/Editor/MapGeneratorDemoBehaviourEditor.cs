using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Map.Unity.Editor
{
    [CustomEditor(typeof(MapGeneratorDemoBehaviour))]
    public sealed class MapGeneratorDemoBehaviourEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var generateButton = new Button(((MapGeneratorDemoBehaviour)target).GenerateAndSaveSample)
            {
                name = "generate-and-save-sample-button",
                text = "Generate And Save Sample"
            };
            root.Add(generateButton);
            return root;
        }
    }
}
