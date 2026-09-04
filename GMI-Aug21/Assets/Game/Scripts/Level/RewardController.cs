using DG.Tweening;
using Oxtail.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    [RequireComponent(typeof(Button))]
    public class RewardController : MonoBehaviour
    {
        [SerializeField] private bool m_IsVideoReward;
        [SerializeField] private FloatingText m_TextPrefab;

        [Header("Audio")]
        [SerializeField] private AudioClip m_RewardSound;

        private Button m_Button;
        private RewardType m_RewardType;


        private void Awake()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(()=> RewardPressed());
        }

        private void OnDestroy()
        {
            transform.RectTransform().DOKill();
        }

        public void SetDestination(Vector2 pos)
        {
            transform.RectTransform().DOAnchorPosY(pos.y + 15f, 1f).SetLoops(-1, LoopType.Yoyo);
            transform.RectTransform().DOMoveX(pos.x, 20f).SetEase(Ease.Linear)
            .OnComplete(()=>
            {
                transform.RectTransform().DOKill();
                Destroy(gameObject);
            });
        }

        public void SetType(RewardType type)
        {
            m_RewardType = type;
        }

        private void RewardPressed()
        {
            AudioManager.Instance.PlaySound(m_RewardSound);

            m_Button.interactable = false;
            transform.RectTransform().DOKill();

            if (m_IsVideoReward)
                DoVideoRewardPressed();
            else
            {
                DoRewardPressed();
                Destroy(gameObject);
            }
        }

        private void DoRewardPressed()
        {
            var text = FloatingTextPooler.Instance.GetText();
            text.transform.position = transform.position;

            switch (m_RewardType)
            {
                case RewardType.SpeedTime:
                    text.SetText("20s SPEED UP!");
                    text.SetColor(Color.green);
                    break;
                case RewardType.Coins:
                    text.SetText($"+{GameUpgradesCostSO.Instance.GetAddSpaceshipUpgradeCost()} COINS");
                    text.SetColor(Color.yellow);
                    break;
                case RewardType.DoubleCoins:
                    text.SetText("10s DOUBLE COINS!");
                    text.SetColor(Color.cyan);
                    break;
                case RewardType.FreeMerge:
                    text.SetText("FREE MERGE!");
                    text.SetColor(Color.magenta);
                    break;
            }

            text.gameObject.SetActive(true);

            LevelManager.Instance.ApplyReward(m_RewardType);
        }

        private void DoVideoRewardPressed()
        {
            var context = new Dictionary<string, object>();
            context.Add("RV Gift", "Gift pressed");

            var text = FloatingTextPooler.Instance.GetText();
            text.transform.position = transform.position;

            switch (m_RewardType)
            {
                case RewardType.SpeedTime:
                    text.SetText("60s SPEED UP!");
                    text.SetColor(Color.green);
                    break;
                case RewardType.Coins:
                    text.SetText($"+{GameUpgradesCostSO.Instance.GetAddSpaceshipUpgradeCost() * 3f} COINS");
                    text.SetColor(Color.yellow);
                    break;
                case RewardType.DoubleCoins:
                    text.SetText("30s DOUBLE COINS!");
                    text.SetColor(Color.cyan);
                    break;
                case RewardType.FreeMerge:
                    text.SetText("3 FREE MERGE!");
                    text.SetColor(Color.magenta);
                    break;
            }

            text.gameObject.SetActive(true);

            LevelManager.Instance.ApplyVideoReward(m_RewardType);

            Destroy(gameObject);
        }
    }
}
