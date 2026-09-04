using UnityEditor;
using UnityEngine;

namespace Oxtail.Utils
{
    [CustomPropertyDrawer(typeof(BigNumber))]
    public class BigNumberDrawer : PropertyDrawer
    {
        private string m_CachedString;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var mantissaProp = property.FindPropertyRelative("m_Mantissa");
            var exponentProp = property.FindPropertyRelative("m_Exponent");

            BigNumber current = new BigNumber
            {
                m_Mantissa = mantissaProp.doubleValue,
                m_Exponent = exponentProp.intValue
            };

            if (string.IsNullOrEmpty(m_CachedString))
                m_CachedString = current.ToFullString();

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();

            m_CachedString = EditorGUI.TextField(position, label, m_CachedString);

            if (EditorGUI.EndChangeCheck())
            {
                BigNumber parsed = BigNumber.FromString(m_CachedString);

                mantissaProp.doubleValue = parsed.m_Mantissa;
                exponentProp.intValue = parsed.m_Exponent;
            }

            EditorGUI.EndProperty();
        }
    }
}
