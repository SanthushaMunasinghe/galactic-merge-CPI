using Oxtail.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class MapSelectionView : MonoBehaviour
    {
        [SerializeField] private Button m_Button;
        [SerializeField] private TMP_Text m_MapNumber;
        [SerializeField] private GameObject m_CurrentTextView;
        [SerializeField] private GameObject m_LockedView;

        public void SetIndex(int index)
        {
            m_MapNumber.text = (index + 1).ToString();

            int maxLevelUnlocked = SaveLoadManager.Instance.GetMaxLevelUnlockedIndex();
            int currentLevel = SaveLoadManager.Instance.GetLevelIndex();

            m_Button.interactable = index <= maxLevelUnlocked && index != currentLevel;
            m_CurrentTextView.SetActive(index == currentLevel);
            m_LockedView.SetActive(index > maxLevelUnlocked);

            m_Button.onClick.RemoveAllListeners();
            m_Button.onClick.AddListener(()=> OnButtonClicked(index));
        }

        private void OnButtonClicked(int index)
        {
            SaveLoadManager.Instance.ResetLevelData();
            SaveLoadManager.Instance.SaveLevelIndex(index);
            GameSceneManager.LoadScene(GameScenesSO.Instance.GameScene);
        }
    }
}
