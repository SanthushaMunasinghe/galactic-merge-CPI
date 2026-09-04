using UnityEditor;
using UnityEngine;

namespace Oxtail.Utils
{
    [CustomPropertyDrawer(typeof(SortingLayerAttribute))]
    public class SortingLayerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                var sortingLayers = SortingLayer.layers;
                string[] layerNames = new string[sortingLayers.Length];
                int currentIndex = 0;

                for (int i = 0; i < sortingLayers.Length; i++)
                {
                    layerNames[i] = sortingLayers[i].name;
                    if (property.stringValue == sortingLayers[i].name)
                    {
                        currentIndex = i;
                    }
                }

                int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, layerNames);
                property.stringValue = layerNames[selectedIndex];
            }
            else
                EditorGUI.LabelField(position, label.text, "Use [SortingLayer] with string.");
        }
    }
}
