using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class DailyLoginController : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private RectTransform m_RewardsPanel;
        [SerializeField] private Button m_ClaimButton;

        [Header("Today Reward")]
        [SerializeField] private Image m_TodayIcon;
        [SerializeField] private HorizontalLayoutGroup m_TodayLayout;
        [SerializeField] private TMP_Text m_TodayText;

        [Header("Tomorrow Reward")]
        [SerializeField] private RectTransform m_TomorrowView;
        [SerializeField] private Image m_TomorrowIcon;
        [SerializeField] private HorizontalLayoutGroup m_TomorrowLayout;
        [SerializeField] private TMP_Text m_TomorrowText;

        [Header("Streak Reward")]
        [SerializeField] private RectTransform m_StreakView;
        [SerializeField] private Image m_StreakIcon;
        [SerializeField] private HorizontalLayoutGroup m_StreakLayout;
        [SerializeField] private TMP_Text m_StreakTittleText;
        [SerializeField] private TMP_Text m_StreakText;

        private void Awake()
        {
            m_ClaimButton.onClick.AddListener(()=> HidePanel());
        }

        private void OnEnable()
        {
            if (!SaveLoadManager.Instance.GetShowLoginReward())
                return;

            int week = SaveLoadManager.Instance.GetRewardsWeekIndex();
            DailyLoginRewardsSO weekRewards = GameDailyRewardsSO.Instance.GetLoginWeekRewards(week);
            int loginDay = SaveLoadManager.Instance.GetLoginDay();

            ShowTodayReward(weekRewards.GetDayReward(loginDay));

            if (loginDay < 6)
                ShowTomorrowReward(weekRewards.GetDayReward(loginDay + 1));
            else
            {
                m_TomorrowView.gameObject.SetActive(loginDay < 6);
                m_RewardsPanel.sizeDelta -= new Vector2(0f, m_TomorrowView.sizeDelta.y);
            }

            if (loginDay < 7)
                ShowStreakReward(weekRewards.GetDayReward(7), 7 - loginDay);
            else
            {
                m_StreakView.gameObject.SetActive(false);
                m_RewardsPanel.sizeDelta -= new Vector2(0f, m_StreakView.sizeDelta.y);
            }

            m_Panel.SetActive(true);
            SaveLoadManager.Instance.SaveShowLoginReward(false);
            SaveLoadManager.Instance.SaveLoginDay(loginDay + 1);

            StartCoroutine(AutoClaim());
        }

        private void ShowTodayReward(GameRewardInfo info)
        {
            SetInfoView(m_TodayIcon, m_TodayText, m_TodayLayout, info);

            if (info.Type == GameRewardType.PowerUpUnlock)
            {
                if (SaveLoadManager.Instance.GetPowerUpLevel(info.PowerUp.PowerUpID) == 0)
                {
                    SaveLoadManager.Instance.SavePowerUpLevel(info.PowerUp.PowerUpID, 1);
                    SaveLoadManager.Instance.SavePowerUpUnlockPercentage(info.PowerUp.PowerUpID, 100);
                }
            }
            else
                SaveLoadManager.Instance.AddGems(info.Quantity);
        }

        private void ShowTomorrowReward(GameRewardInfo info)
        {
            SetInfoView(m_TomorrowIcon, m_TomorrowText, m_TomorrowLayout, info);
        }

        private void ShowStreakReward(GameRewardInfo info, int remainingDays)
        {
            SetInfoView(m_StreakIcon, m_StreakText, m_StreakLayout, info);
            if (remainingDays > 0)
                m_StreakTittleText.text += $"<br><color=#FF8400>{remainingDays}</color> days left!";
        }

        private void SetInfoView(Image icon, TMP_Text text, HorizontalLayoutGroup layout, GameRewardInfo info)
        {
            if (info.Type == GameRewardType.PowerUpUnlock)
            {
                layout.childControlWidth = false;
                icon.sprite = info.PowerUp.PowerUpIcon;
                icon.rectTransform.sizeDelta = new Vector2 (70f, 70f);
                text.text = info.PowerUp.PowerUpDescription;
                text.fontSize = 25f;
                text.rectTransform.sizeDelta = new Vector2(200f, text.rectTransform.sizeDelta.y);
            }
            else
                text.text = info.Quantity.ToString();
        }

        private IEnumerator AutoClaim()
        {
            yield return new WaitForSeconds(10f);

            HidePanel();
        }

        private void HidePanel()
        {
            m_Panel.gameObject.SetActive(false);
        }
    }
}
