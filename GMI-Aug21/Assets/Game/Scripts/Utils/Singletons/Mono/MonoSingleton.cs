using UnityEngine;

namespace Oxtail.Utils
{
    public class MonoSingleton<T> : MonoBehaviour where T : Component
    {
        protected static T m_Instance;

        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = FindAnyObjectByType<T>();

                return m_Instance;
            }
        }
                
        protected virtual void Awake()
        {
            if (m_Instance is null)
                m_Instance = this as T;
        }

        private void OnDestroy()
        {
            m_Instance = null;
        }
    }
}
