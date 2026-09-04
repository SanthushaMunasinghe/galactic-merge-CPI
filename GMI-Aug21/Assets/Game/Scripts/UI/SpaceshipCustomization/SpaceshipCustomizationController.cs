using Oxtail.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public struct UpdateShapePreviewEvent
    {
        public string ShapeID { get; private set; }

        public UpdateShapePreviewEvent(string shapeID)
        {
            ShapeID = shapeID;
        }
    }

    public struct UpdateEffectPreviewEvent
    {
        public string EffectID { get; private set; }

        public UpdateEffectPreviewEvent(string effectID)
        {
            EffectID = effectID;
        }
    }

    public struct UpdateTrailPreviewEvent
    {
        public string TrailID { get; private set; }

        public UpdateTrailPreviewEvent(string trailID)
        {
            TrailID = trailID;
        }
    }

    public class SpaceshipCustomizationController : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private Button m_CustomizationButton;
        [SerializeField] private Button m_CloseButton;

        [Header("Shapes")]
        [SerializeField] private Button m_ShapesMenuButton;
        [SerializeField] private GameObject m_ShapesPanel;
        [SerializeField] private GridLayoutGroup m_ShapesListParent;

        [Header("Effects")]
        [SerializeField] private Button m_EffectsMenuButton;
        [SerializeField] private GameObject m_EffectsPanel;
        [SerializeField] private GridLayoutGroup m_EffectsListParent;

        [Header("Trails")]
        [SerializeField] private Button m_TrailsMenuButton;
        [SerializeField] private GameObject m_TrailsPanel;
        [SerializeField] private GridLayoutGroup m_TrailsListParent;

        [Header("Prefab")]
        [SerializeField] private CustomizationView m_CustomizationViewPrefab;

        [Header("Selection Color")]
        [SerializeField] private Color m_MenuButtonSelectedColor;
        [SerializeField] private Color m_MenuButtonUnselectedColor;

        private List<CustomizationView> m_ShapesCreated = new();
        private List<CustomizationView> m_EffectsCreated = new();
        private List<CustomizationView> m_TrailsCreated = new();

        private void Awake()
        {
            m_CustomizationButton.onClick.AddListener(()=> ShowPanel());
            m_CloseButton.onClick.AddListener(()=> HidePanel());

            m_ShapesMenuButton.onClick.AddListener(()=> ShowShapes());
            m_EffectsMenuButton.onClick.AddListener(()=> ShowEffects());
            m_TrailsMenuButton.onClick.AddListener(()=> ShowTrails());
        }

        private void OnDestroy()
        {
            SaveLoadManager.Instance.OnGemsUpdated -= UpdateViews;
            SaveLoadManager.Instance.OnCelestiumUpdated -= UpdateViews;

            foreach (var shape in m_ShapesCreated)
            {
                shape.OnPreviewClicked -= UpdateShapePreview;
            }
            foreach (var effect in m_EffectsCreated)
            {
                effect.OnPreviewClicked -= UpdateEffectPreview;
            }
            foreach (var trail in m_TrailsCreated)
            {
                trail.OnPreviewClicked -= UpdateTrailPreview;
            }
        }

        private void Start()
        {
            CreateShapes();
            CreateEffects();
            CreateTrails();

            SaveLoadManager.Instance.OnGemsUpdated += UpdateViews;
            SaveLoadManager.Instance.OnCelestiumUpdated += UpdateViews;
        }

        private void UpdateViews()
        {
            string playerShapeID = SaveLoadManager.Instance.GetSpaceshipShapeID();
            var shapes = SpaceshipCustomizationSO.Instance.Shapes;
            for (int i = 0; i < shapes.Length; i++)
            {
                var shape = m_ShapesCreated[i];
                shape.SetData(shapes[i]);
                if (shape.CustomizationID == playerShapeID)
                    shape.SetCurrentCustomization();
            }

            string playerEffectID = SaveLoadManager.Instance.GetSpaceshipEffectID();
            var effects = SpaceshipCustomizationSO.Instance.Effects;
            for (int i = 0; i < effects.Length; i++)
            {
                var effect = m_EffectsCreated[i];
                effect.SetData(effects[i]);
                if (effect.CustomizationID == playerEffectID)
                    effect.SetCurrentCustomization();
            }

            string playerTrailID = SaveLoadManager.Instance.GetSpaceshipTrailID();
            var trails = SpaceshipCustomizationSO.Instance.Trails;
            for (int i = 0; i < trails.Length; i++)
            {
                var trail = m_TrailsCreated[i];
                trail.SetData(trails[i]);
                if (trail.CustomizationID == playerTrailID)
                    trail.SetCurrentCustomization();
            }
        }

        private void ShowPanel()
        {
            m_Panel.SetActive(true);
            ShowShapes();
        }

        private void HidePanel()
        {
            m_Panel.SetActive(false);
        }

        private void CreateShapes()
        {
            string playerShapeID = SaveLoadManager.Instance.GetSpaceshipShapeID();

            foreach (var shapeData in SpaceshipCustomizationSO.Instance.Shapes)
            {
                var shapeView = Instantiate(m_CustomizationViewPrefab, m_ShapesListParent.transform);
                shapeView.SetData(shapeData);
                m_ShapesCreated.Add(shapeView);
                shapeView.OnPreviewClicked += UpdateShapePreview;
                shapeView.OnCustomizationSelected += OnShapeCustomizationSelected;

                if (playerShapeID == shapeData.CustomizationID)
                {
                    shapeView.SetPreviewSelected();
                    shapeView.SetCurrentCustomization();
                    UpdateShapePreview(playerShapeID);
                }
            }
        }

        private void OnShapeCustomizationSelected(CustomizationView view)
        {
            foreach (var shapeView in m_ShapesCreated)
            {
                if (shapeView.CustomizationID == view.CustomizationID)
                    shapeView.SetCurrentCustomization();
                else
                    shapeView.UnsetCurrentCustomization();
            }
        }

        private void UpdateShapePreview(string shapeID)
        {
            EventManager<UpdateShapePreviewEvent>.TriggerEvent(new UpdateShapePreviewEvent(shapeID));

            foreach (var shapeView in m_ShapesCreated)
            {
                if (shapeView.CustomizationID == shapeID)
                    shapeView.SetPreviewSelected();
                else
                    shapeView.SetPreviewUnselected();
            }
        }

        private void CreateEffects()
        {
            string playerEffectID = SaveLoadManager.Instance.GetSpaceshipEffectID();

            foreach (var effectData in SpaceshipCustomizationSO.Instance.Effects)
            {
                var effectView = Instantiate(m_CustomizationViewPrefab, m_EffectsListParent.transform);
                effectView.SetData(effectData);
                m_EffectsCreated.Add(effectView);
                effectView.OnPreviewClicked += UpdateEffectPreview;
                effectView.OnCustomizationSelected += OnEffectCustomizationSelected;

                if (playerEffectID == effectData.CustomizationID)
                {
                    effectView.SetPreviewSelected();
                    effectView.SetCurrentCustomization();
                    UpdateEffectPreview(playerEffectID);
                }
            }
        }

        private void OnEffectCustomizationSelected(CustomizationView view)
        {
            foreach (var effectView in m_EffectsCreated)
            {
                if (effectView.CustomizationID == view.CustomizationID)
                    effectView.SetCurrentCustomization();
                else
                    effectView.UnsetCurrentCustomization();
            }
        }

        private void UpdateEffectPreview(string effectID)
        {
            EventManager<UpdateEffectPreviewEvent>.TriggerEvent(new UpdateEffectPreviewEvent(effectID));

            foreach (var effectView in m_EffectsCreated)
            {
                if (effectView.CustomizationID == effectID)
                    effectView.SetPreviewSelected();
                else
                    effectView.SetPreviewUnselected();
            }
        }

        private void CreateTrails()
        {
            string playerTrailID = SaveLoadManager.Instance.GetSpaceshipTrailID();

            foreach (var trailData in SpaceshipCustomizationSO.Instance.Trails)
            {
                var trailView = Instantiate(m_CustomizationViewPrefab, m_TrailsListParent.transform);
                trailView.SetData(trailData);
                m_TrailsCreated.Add(trailView);
                trailView.OnPreviewClicked += UpdateTrailPreview;
                trailView.OnCustomizationSelected += OnTrailCustomizationSelected;

                if (playerTrailID == trailData.CustomizationID)
                {
                    trailView.SetPreviewSelected();
                    trailView.SetCurrentCustomization();
                    UpdateTrailPreview(playerTrailID);
                }
            }
        }

        private void OnTrailCustomizationSelected(CustomizationView view)
        {
            foreach (var trailView in m_TrailsCreated)
            {
                if (trailView.CustomizationID == view.CustomizationID)
                    trailView.SetCurrentCustomization();
                else
                    trailView.UnsetCurrentCustomization();
            }
        }

        private void UpdateTrailPreview(string trailID)
        {
            EventManager<UpdateTrailPreviewEvent>.TriggerEvent(new UpdateTrailPreviewEvent(trailID));

            foreach (var trailView in m_TrailsCreated)
            {
                if (trailView.CustomizationID == trailID)
                    trailView.SetPreviewSelected();
                else
                    trailView.SetPreviewUnselected();
            }
        }

        private void ShowShapes()
        {
            m_ShapesPanel.SetActive(true);
            m_EffectsPanel.SetActive(false);
            m_TrailsPanel.SetActive(false);

            m_ShapesMenuButton.image.color = m_MenuButtonSelectedColor;
            m_EffectsMenuButton.image.color = m_MenuButtonUnselectedColor;
            m_TrailsMenuButton.image.color = m_MenuButtonUnselectedColor;
        }

        private void ShowEffects()
        {
            m_ShapesPanel.SetActive(false);
            m_EffectsPanel.SetActive(true);
            m_TrailsPanel.SetActive(false);

            m_ShapesMenuButton.image.color = m_MenuButtonUnselectedColor;
            m_EffectsMenuButton.image.color = m_MenuButtonSelectedColor;
            m_TrailsMenuButton.image.color = m_MenuButtonUnselectedColor;
        }

        private void ShowTrails()
        {
            m_ShapesPanel.SetActive(false);
            m_EffectsPanel.SetActive(false);
            m_TrailsPanel.SetActive(true);

            m_ShapesMenuButton.image.color = m_MenuButtonUnselectedColor;
            m_EffectsMenuButton.image.color = m_MenuButtonUnselectedColor;
            m_TrailsMenuButton.image.color = m_MenuButtonSelectedColor;
        }
    }
}
