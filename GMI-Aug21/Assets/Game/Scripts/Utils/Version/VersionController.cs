using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Oxtail.Utils
{
    public class VersionController : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_VersionText;

        private void Awake()
        {
            m_VersionText.text = Application.version;
        }
    }
}
