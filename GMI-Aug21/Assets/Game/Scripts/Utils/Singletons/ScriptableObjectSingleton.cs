using UnityEditor;
using UnityEngine;

namespace Oxtail.Utils
{
    public class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T m_Instance;

        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
#if UNITY_EDITOR
                    string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        m_Instance = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
                    }
                    else
                        throw new System.Exception($"ScriptableObject Singleton: Asset of type: {typeof(T)} not found.");
#else
                    m_Instance = FindObjectOfType<T>();
#endif
                }

                return m_Instance;
            }
        }

        protected virtual void Awake()
        {
            if (!Application.isPlaying)
                return;

            m_Instance = this as T;
        }
    }
}

