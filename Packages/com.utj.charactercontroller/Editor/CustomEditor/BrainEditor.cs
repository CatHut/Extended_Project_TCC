using Unity.TinyCharacterController.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyCharacterControllerEditor
{
    /// <summary>
    /// Inspector for every Brain.
    /// Shows the Brain's own settings, followed by a list of the TCC components on the same GameObject
    /// grouped by role (<see cref="BrainComponentsView"/>), and runtime information while playing
    /// (<see cref="BrainRuntimeView"/>).
    /// </summary>
    [CustomEditor(typeof(BrainBase), true)]
    public class BrainEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var settings = new Foldout
            {
                text = "Brain Settings",
                value = true,
                viewDataKey = "tcc-brain-settings"
            };
            InspectorElement.FillDefaultInspector(settings, serializedObject, this);
            root.Add(settings);

            if (targets.Length > 1)
            {
                root.Add(new HelpBox("The TCC component list is shown only when a single Brain is selected.",
                    HelpBoxMessageType.Info));
                return root;
            }

            root.Add(new BrainComponentsView((BrainBase)target));
            return root;
        }
    }
}
