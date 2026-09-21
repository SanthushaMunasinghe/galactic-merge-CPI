using UnityEditor;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CustomEditor(typeof(BulletSpawnManager))]
    public class BulletSpawnManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Shot Points"))
                ((BulletSpawnManager)target).GenerateShotPoints();
        }
    }
}
