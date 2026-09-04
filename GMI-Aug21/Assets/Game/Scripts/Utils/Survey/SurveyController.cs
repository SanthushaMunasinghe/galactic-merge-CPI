using Oxtail.SpaceshipIncremental;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Oxtail.Utils
{
    public class SurveyController : MonoBehaviour
    {
        [Header("Button")]
        [SerializeField] private Button m_SurveyButton;

        [Header("Panel")]
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private Button m_SendButton;
        [SerializeField] private Button m_CloseButton;

        [Header("Inputs")]
        [SerializeField] private Toggle m_YesToggle;
        [SerializeField] private Toggle m_NoToggle;
        [SerializeField] private TMP_InputField m_AddToGameInput;
        [SerializeField] private TMP_InputField m_RetentionInput;

        [Header("Survey")]
        [SerializeField, TextArea] private string m_ResponseURL;
        [SerializeField] private string m_LikeGameEntry;
        [SerializeField] private string m_AddToGameEntry;
        [SerializeField] private string m_RetentionEntry;
        [SerializeField] private string m_DeviceEntry;
        [SerializeField] private string m_AppVersionEntry;

        [Header("Result Text")]
        [SerializeField] private TMP_Text m_ResultText;

        [Header("Gems Reward")]
        [SerializeField] private int m_GemsReward;

        private void Awake()
        {
            m_YesToggle.isOn = true;
            m_YesToggle.onValueChanged.AddListener(YesToggleChanged);

            m_NoToggle.isOn = false;
            m_NoToggle.onValueChanged.AddListener(NoToggleChanged);

            m_SendButton.onClick.AddListener(()=> SendSurvey());
            m_CloseButton.onClick.AddListener(()=> ClosePanel());
            m_SurveyButton.onClick.AddListener(()=> ShowSurvey());
        }

        private void OnEnable()
        {
            if (SaveLoadManager.Instance.GetSurveySent())
            {
                m_SurveyButton.gameObject.SetActive(false);
                return;
            }

            bool show = SaveLoadManager.Instance.GetLoginDay() > 2 
                || SaveLoadManager.Instance.GetRewardsWeekIndex() > 0;

            m_SurveyButton.gameObject.SetActive(show);
        }

        private void YesToggleChanged(bool value)
        {
            m_NoToggle.SetIsOnWithoutNotify(!value);
        }

        private void NoToggleChanged(bool value)
        {
            m_YesToggle.SetIsOnWithoutNotify(!value);
        }

        private void ShowSurvey()
        {
            m_Panel.SetActive(true);
        }

        private void SendSurvey()
        {
            m_ResultText.text = string.Empty;
            m_ResultText.color = Color.white;

            if (string.IsNullOrEmpty(m_AddToGameInput.text))
            {
                m_ResultText.text = "The answer to the first question is empty";
                m_ResultText.color = Color.red;
                return;
            }

            if (string.IsNullOrEmpty(m_RetentionInput.text))
            {
                m_ResultText.text = "The answer to the second question is empty";
                m_ResultText.color = Color.red;
                return;
            }

            StartCoroutine(PostData());
        }

        private IEnumerator PostData()
        {
            m_SendButton.interactable = false;

            WWWForm form = new WWWForm();

            form.AddField($"entry.{m_LikeGameEntry}", m_YesToggle.isOn ? "Yes" : "No");
            form.AddField($"entry.{m_AddToGameEntry}", m_AddToGameInput.text);
            form.AddField($"entry.{m_RetentionEntry}", m_RetentionInput.text);
            form.AddField($"entry.{m_DeviceEntry}", SystemInfo.deviceModel);
            form.AddField($"entry.{m_AppVersionEntry}", Application.version);

            using (UnityWebRequest www = UnityWebRequest.Post(m_ResponseURL, form))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Survey Error: " + www.error);
                    m_ResultText.text = www.error;
                    m_ResultText.color = Color.red;
                    m_SendButton.interactable = false;
                }
                else
                {
                    Debug.Log("Survey Send!");
                    m_ResultText.text = "Survey Send!";
                    m_ResultText.color = Color.green;
                    SaveLoadManager.Instance.AddGems(m_GemsReward);
                    SaveLoadManager.Instance.SaveSurveySent();
                    m_SurveyButton.gameObject.SetActive(false);
                    ClosePanel();
                }
            }
        }

        private void ClosePanel()
        {
            m_ResultText.text = string.Empty;
            m_Panel.SetActive(false);
        }
    }
}
