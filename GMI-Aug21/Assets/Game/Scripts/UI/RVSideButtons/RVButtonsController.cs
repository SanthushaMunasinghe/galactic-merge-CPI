using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class RVButtonsController : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private CanvasGroup m_Canvas;
        [SerializeField] private GameObject m_OfflinePanel;
        [SerializeField] private GameObject m_DailyPanel;

        [Space]
        [SerializeField] private Button m_AddSpaceshipButton;
        [SerializeField] private Button m_AddCoinsButton;
        [SerializeField] private Button m_SpeedUpButton;
        [SerializeField] private Button m_FleetButton;

        [Space]
        [SerializeField] private Transform m_FloatingTextPos;

        private bool m_Initfinished;

        private void Awake()
        {
            m_AddSpaceshipButton.onClick.AddListener(()=> DoAddSpaceshipRV());
            m_AddCoinsButton.onClick.AddListener(()=> DoAddCoinsRV());
            m_SpeedUpButton.onClick.AddListener(()=> DoSpeedUpRV());
            m_FleetButton.onClick.AddListener(()=> DoFleetRV());
        }

        private void Start()
        {
            m_AddSpaceshipButton.gameObject.SetActive(false);
            m_AddCoinsButton.gameObject.SetActive(false);
            m_SpeedUpButton.gameObject.SetActive(false);

            LevelManager.Instance.CanSpawnSpaceship.OnPropertyChanged += SpaceShipsAmountChanged;

            if (Debugger.FakeRewardedVideos) OnRVAvailable(true);
        }

        private void OnDestroy()
        {
            LevelManager.Instance.CanSpawnSpaceship.OnPropertyChanged -= SpaceShipsAmountChanged;
        }

        private void OnRVAvailable(bool complete)
        {
            if (!complete)
                return;

            if (!gameObject.activeInHierarchy)
                return;

            StartCoroutine(ShowAds());
        }

        private IEnumerator ShowAds()
        {
            float fleetPercentage = Debugger.ShowFleetPercentage;

            if (LevelGenerator.Instance.PlayedOtherLevelBefore)
            {
                if (Debugger.ShowTutorial)
                {
                    yield return new WaitUntil(() =>
                    SaveLoadManager.Instance.GetInitialTutorial1Done() &&
                    SaveLoadManager.Instance.GetInitialTutorial2Done() &&
                    !m_OfflinePanel.activeInHierarchy &&
                    !m_DailyPanel.activeInHierarchy);
                }
                else
                {
                    if (LevelManager.Instance.LevelIndex == 0)
                    {
                        yield return new WaitUntil(() => !m_OfflinePanel.activeInHierarchy &&
                        !m_DailyPanel.activeInHierarchy);

                        if (LevelManager.Instance.LevelProgressionStepIndex == 0)
                            yield return new WaitForSeconds(5f);
                    }
                }

                m_AddCoinsButton.gameObject.SetActive(true);
                m_SpeedUpButton.gameObject.SetActive(true);

                if (fleetPercentage > 0 && LevelManager.Instance.SpaceshipFreePercentage >= fleetPercentage)
                {
                    m_AddSpaceshipButton.gameObject.SetActive(false);
                    m_FleetButton.gameObject.SetActive(false);
                }
                else
                    m_AddSpaceshipButton.gameObject.SetActive(true);
            }
            else
            {
                if (Debugger.ShowTutorial)
                {
                    yield return new WaitUntil(() => m_Canvas.alpha >= 1 &&
                    SaveLoadManager.Instance.GetInitialTutorial1Done() &&
                    SaveLoadManager.Instance.GetInitialTutorial2Done() &&
                    !m_OfflinePanel.activeInHierarchy &&
                    !m_DailyPanel.activeInHierarchy);
                }
                else
                {
                    if (LevelManager.Instance.LevelIndex == 0)
                    {
                        yield return new WaitUntil(()=> !m_OfflinePanel.activeInHierarchy &&
                        !m_DailyPanel.activeInHierarchy);

                        if (LevelManager.Instance.LevelProgressionStepIndex == 0)
                            yield return new WaitForSeconds(5f);
                    }
                }

                m_AddCoinsButton.gameObject.SetActive(false);
                m_SpeedUpButton.gameObject.SetActive(false);
                m_AddSpaceshipButton.gameObject.SetActive(false);
                m_FleetButton.gameObject.SetActive(false);

                yield return new WaitForSeconds(1f);
                if (fleetPercentage > 0 && LevelManager.Instance.SpaceshipFreePercentage >= fleetPercentage)
                {
                    m_FleetButton.transform.localScale = Vector3.one;
                    m_FleetButton.gameObject.SetActive(true);
                }
                else
                {
                    m_AddSpaceshipButton.transform.localScale = Vector3.one;
                    m_AddSpaceshipButton.gameObject.SetActive(true);
                }

                yield return new WaitForSeconds(2.5f);
                m_SpeedUpButton.transform.localScale = Vector3.one;
                m_SpeedUpButton.gameObject.SetActive(true);

                yield return new WaitForSeconds(2f);
                m_AddCoinsButton.transform.localScale = Vector3.one;
                m_AddCoinsButton.gameObject.SetActive(true);
            }

            m_Initfinished = true;
        }

        private void SpaceShipsAmountChanged(bool canSpawn)
        {
            if (!m_Initfinished)
                return;

            float fleetPercentage = Debugger.ShowFleetPercentage;

            if (fleetPercentage > 0 && LevelManager.Instance.SpaceshipFreePercentage >= fleetPercentage)
            {
                if (m_FleetButton.gameObject.activeInHierarchy)
                    return;

                m_AddSpaceshipButton.gameObject.SetActive(false);
                m_FleetButton.gameObject.SetActive(false);
                m_FleetButton.GetComponent<Animator>().Rebind();
                m_FleetButton.transform.localScale = Vector3.one;
                m_FleetButton.gameObject.SetActive(true);
            }
            else
            {
                if (m_AddSpaceshipButton.gameObject.activeInHierarchy)
                    return;

                m_AddSpaceshipButton.gameObject.SetActive(false);
                m_FleetButton.gameObject.SetActive(false);
                m_AddSpaceshipButton.transform.localScale = Vector3.one;
                m_AddSpaceshipButton.gameObject.SetActive(true);
            }
        }

        private void DoAddSpaceshipRV()
        {
            if (!LevelManager.Instance.CanSpawnSpaceship.Value)
            {
                LevelManager.Instance.MergeSpaceship();
                LevelManager.Instance.AddNextTierSpaceship();
            }

            StartCoroutine(DoButtonCooldown(m_AddSpaceshipButton.gameObject));

            ShowFloatingText("ADDED SPACESHIP!", Color.cyan);
        }

        private void DoAddCoinsRV()
        {
            LevelManager.Instance.ApplyVideoReward(RewardType.Coins);

            StartCoroutine(DoButtonCooldown(m_AddCoinsButton.gameObject));

            ShowFloatingText($"+{GameUpgradesCostSO.Instance.GetAddSpaceshipUpgradeCost() * 3f} COINS", Color.yellow);
        }

        private void DoSpeedUpRV()
        {
            LevelManager.Instance.ApplyVideoReward(RewardType.SpeedTime);

            StartCoroutine(DoButtonCooldown(m_SpeedUpButton.gameObject));

            ShowFloatingText("60s SPEED UP!", Color.green);
        }

        private void DoFleetRV()
        {
            while (LevelManager.Instance.CanSpawnSpaceship.Value)
            {
                LevelManager.Instance.AddDefaultSpaceship();
            }

            StartCoroutine(DoButtonCooldown(m_FleetButton.gameObject));

            ShowFloatingText("FLEET INCOMING", Color.magenta);
        }

        private IEnumerator DoButtonCooldown(GameObject button)
        {
            int minutes = Debugger.SideButtonCooldown;
            
            if (minutes > 0)
            {
                button.SetActive(false);

                yield return new WaitForSeconds(minutes * 60f);

                button.SetActive(true);
            }
        }

        private void ShowFloatingText(string text, Color color)
        {
            var floatingText = FloatingTextPooler.Instance.GetText();
            floatingText.transform.position = m_FloatingTextPos.position;

            floatingText.SetText(text);
            floatingText.SetColor(color);
            floatingText.gameObject.SetActive(true);
        }
    }
}
