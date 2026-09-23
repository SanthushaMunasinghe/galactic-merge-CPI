using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.Utils
{
    public class PersistentMonoSingleton<T> : MonoSingleton<T> where T : Component
    {
        protected override void Awake()
        {
            if (m_Instance != null && m_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            base.Awake();
            DontDestroyOnLoad(gameObject);
        }
    }
}
