using UnityEditor;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CustomEditor(typeof(AsteroidSpawnManager))]
    public class AsteroidSpawnManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AsteroidSpawnManager spawnManager = (AsteroidSpawnManager)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Manual Wave Spawn Points"))
                spawnManager.GenerateManualWaveSpawnPoints();
        }
    }
}
