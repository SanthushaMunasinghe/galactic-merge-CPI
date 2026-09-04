using UnityEditor;
using UnityEngine;

namespace Oxtail.Utils
{
    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public class ScenePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [Scene] with string.");
                return;
            }

            SceneAsset sceneAsset = null;
            if (!string.IsNullOrEmpty(property.stringValue))
            {
                string[] guids = AssetDatabase.FindAssets(property.stringValue + " t:Scene");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                }
            }

            SceneAsset selectedScene = EditorGUI.ObjectField(position, label, sceneAsset, typeof(SceneAsset), false) as SceneAsset;

            if (selectedScene != null)
            {
                string selectedPath = AssetDatabase.GetAssetPath(selectedScene);
                property.stringValue = System.IO.Path.GetFileNameWithoutExtension(selectedPath);
            }
            else
                property.stringValue = "";
        }
    }
}
