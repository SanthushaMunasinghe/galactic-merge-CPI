using DG.Tweening;
using Oxtail.Utils;
using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class Planet : MonoBehaviour
    {
        [SerializeField] private int m_PlanetIndex;

        [Space]
        [SerializeField] private SpriteRenderer m_GreyscalePlanet;
        [SerializeField] private SpriteRenderer m_NormalPlanet;

        [Header("Effect")]
        [SerializeField] private SandDissolveEffect m_DissolveEffect;

        [Header("Particle System")]
        [SerializeField] private ParticleSystem m_HealParticles;

        private BigNumber m_PlanetLife;

        private Vector3 m_InitialScale;

        public BigNumber CurrentHeal { get; private set; }
        public bool FullyHealed { get; private set; }

        private void Awake()
        {
            // Only the normal planet is instanced. The greyscale renderer's material belongs to
            // SandDissolveEffect, which assigns it as sharedMaterial and keeps writing its settings
            // (grayscale keyword and tint, edge, pattern) to that exact object. Replacing it here with a
            // copy detached the renderer from the effect, so those writes landed on a material nothing
            // was drawing with and the grayscale tint silently dropped — and because the two Awakes sit
            // on different GameObjects, which planet lost it was down to Unity's undefined ordering.
            m_NormalPlanet.material = new Material(m_NormalPlanet.material);
        }

        private void Start()
        {
            m_InitialScale = transform.localScale;
        }

        private void OnDestroy()
        {
            transform.DOKill();
            m_NormalPlanet.transform.DOKill();
        }

        public void SetPlanetLife(BigNumber life)
        {
            m_PlanetLife = life;
            CurrentHeal = SaveLoadManager.Instance.GetPlanetHeal(m_PlanetIndex);
            UpdateDisintegrateAmount(BigNumber.Ratio(CurrentHeal, m_PlanetLife));
        }

        public void HealPlanet(int heal)
        {
            if (FullyHealed)
                return;

            CurrentHeal = BigNumber.Min(CurrentHeal + heal, m_PlanetLife);
            SaveLoadManager.Instance.SavePlanetHeal(m_PlanetIndex, CurrentHeal);

            UpdateDisintegrateAmount(BigNumber.Ratio(heal, m_PlanetLife));

            m_HealParticles.Play();
            transform.localScale = m_InitialScale;
            transform.DOKill();
            transform.DOPunchScale(transform.localScale * 0.05f, 0.2f)
                .OnComplete(()=>
                {
                    transform.localScale = m_InitialScale;
                });
        }

        public void DrainPlanet(int drain)
        {
            if (FullyHealed)
                return;

            CurrentHeal = BigNumber.Max(CurrentHeal - drain, 0);
            SaveLoadManager.Instance.SavePlanetHeal(m_PlanetIndex, CurrentHeal);

            UpdateReintegrateAmount(BigNumber.Ratio(drain, m_PlanetLife));
        }

        private void UpdateDisintegrateAmount(float percentageToRemove)
        {
            m_DissolveEffect.Disintegrate(percentageToRemove);

            float healthPercentage = BigNumber.Ratio(CurrentHeal, m_PlanetLife);

            if (healthPercentage == 1f)
                StartCoroutine(ShowFullyHealed());
        }

        private void UpdateReintegrateAmount(float percentageToAdd)
        {
            m_DissolveEffect.Reintegrate(percentageToAdd);
        }

        private IEnumerator ShowFullyHealed()
        {
            FullyHealed = true;

            float frequency = 1.1f;
            
            m_NormalPlanet.transform.DOPunchScale(m_NormalPlanet.transform.localScale * 0.2f, 0.2f);
            m_NormalPlanet.material.SetFloat("_ShineFrequency", frequency);

            yield return new WaitForSeconds(frequency);

            m_NormalPlanet.material.SetFloat("_ShineFrequency", 0f);
        }

        public void ResetPlanetSaveInfo()
        {
            SaveLoadManager.Instance.SavePlanetHeal(m_PlanetIndex, BigNumber.Zero);
        }
    }
}
