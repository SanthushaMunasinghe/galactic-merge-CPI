using UnityEditor;
using UnityEditor.UI;

namespace Oxtail.Utils
{
    [CustomEditor(typeof(HoldButton))]
    [CanEditMultipleObjects]
    public class HoldButtonEditor : ButtonEditor
    {
        private SerializedProperty m_FirstDelay;
        private SerializedProperty m_RepeatRate;
        private SerializedProperty m_UseUnscaledTime;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_FirstDelay = serializedObject.FindProperty("m_FirstDelay");
            m_RepeatRate = serializedObject.FindProperty("m_RepeatRate");
            m_UseUnscaledTime = serializedObject.FindProperty("m_UseUnscaledTime");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hold Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_FirstDelay);
            EditorGUILayout.PropertyField(m_RepeatRate);
            EditorGUILayout.PropertyField(m_UseUnscaledTime);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
