using Unity.TinyCharacterController.Smb;
using UnityEditor;
using UnityEngine;

namespace Unity.TinyCharacterControllerEditor
{
    /// <summary>
    /// Inspector of <see cref="AnimatorMoveBehaviour"/>.
    /// Adds a helper to read the humanScale from a model prefab and write it to ReferenceHumanScale.
    /// Uses IMGUI because StateMachineBehaviours are drawn inside the AnimatorState inspector.
    /// </summary>
    [CustomEditor(typeof(AnimatorMoveBehaviour))]
    public class AnimatorMoveBehaviourEditor : UnityEditor.Editor
    {
        private const string NormalizeHumanScaleName = "_normalizeHumanScale";
        private const string ReferenceHumanScaleName = "_referenceHumanScale";

        private const string HelpMessage =
            "モーション作成に使用したモデル(A)の HumanScale を設定すると、全てのモデルで移動量が A の移動量に統一されます。\n" +
            "Humanoid のアバターにだけ適用されます。";

        // Model used to read the humanScale. Editor only, not saved.
        private GameObject _sourceModel;
        private string _errorMessage;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script", NormalizeHumanScaleName, ReferenceHumanScaleName);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Human Scale Normalization", EditorStyles.boldLabel);

            var normalize = serializedObject.FindProperty(NormalizeHumanScaleName);
            var reference = serializedObject.FindProperty(ReferenceHumanScaleName);
            EditorGUILayout.PropertyField(normalize);

            using (new EditorGUI.DisabledScope(normalize.boolValue == false))
            {
                EditorGUILayout.PropertyField(reference);
                EditorGUILayout.HelpBox(HelpMessage, MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _sourceModel = (GameObject)EditorGUILayout.ObjectField("Model", _sourceModel, typeof(GameObject), false);

                    using (new EditorGUI.DisabledScope(_sourceModel == null))
                    {
                        if (GUILayout.Button("Prefab から取得", GUILayout.Width(110)))
                        {
                            if (TryGetHumanScale(_sourceModel, out var humanScale, out _errorMessage))
                                reference.floatValue = humanScale;
                        }
                    }
                }

                if (string.IsNullOrEmpty(_errorMessage) == false)
                    EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Read the humanScale of the Humanoid Animator in the model asset.
        /// </summary>
        /// <param name="model">Prefab or model asset (FBX) that has an Animator with a Humanoid avatar.</param>
        /// <param name="humanScale">The humanScale of the model.</param>
        /// <param name="error">Error message when failed. Null when succeeded.</param>
        /// <returns>True if the humanScale was read.</returns>
        private static bool TryGetHumanScale(GameObject model, out float humanScale, out string error)
        {
            humanScale = 1f;
            error = null;

            var animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                error = $"{model.name} に Animator がありません。";
                return false;
            }

            if (animator.avatar == null || animator.avatar.isHuman == false)
            {
                error = $"{model.name} の Animator に Humanoid の Avatar が設定されていません。";
                return false;
            }

            humanScale = animator.humanScale;
            if (humanScale <= 0f)
            {
                error = $"{model.name} の humanScale を取得できませんでした。";
                return false;
            }

            return true;
        }
    }
}
