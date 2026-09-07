using DG.Tweening;
using Oxtail.Utils;
using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct UpdateShapeEvent
    {
        public string ShapeID { get; private set; }

        public UpdateShapeEvent(string shapeID)
        {
            ShapeID = shapeID;
        }
    }

    public struct UpdateEffectEvent
    {
        public string EffectID { get; private set; }

        public UpdateEffectEvent(string effectID)
        {
            EffectID = effectID;
        }
    }

    public struct UpdateTrailEvent
    {
        public string TrailID { get; private set; }

        public UpdateTrailEvent(string trailID)
        {
            TrailID = trailID;
        }
    }

    public struct SpaceshipHitRewardLineEvent
    {
        public Spaceship Spaceship;
        public RewardLine Line;
    }

    public class Spaceship : MonoBehaviour
    {
        [SerializeField, TagSelector] private string m_MoneyLineTag;

        [Header("Sorting Layer")]
        [SerializeField, SortingLayer] private string m_DefaultSortingLayer;
        [SerializeField, SortingLayer] private string m_MergeSortingLayer;

        [Header("Merge Effect")]
        [SerializeField] private ParticleSystem m_MergeEffect;

        private SpaceshipShape m_SpaceshipShape;
        private SpaceshipTrail m_SpaceshipTrail;

        private SpaceshipEffectType m_Effect;

        private int m_Coins;
        private bool m_CanMerge = true;

        private Tween m_MergePunchTween;

        public int TierNumber { get; private set; }

        private void Awake()
        {
            if (transform == null)
                return;

            if (m_MergePunchTween != null && !m_MergePunchTween.IsPlaying())
                transform.DOPunchScale(Vector3.one, 0.25f);

            EventManager<UpdateShapeEvent>.AddListener(ShapeChanged);
            EventManager<UpdateEffectEvent>.AddListener(EffectChanged);
            EventManager<UpdateTrailEvent>.AddListener(TrailChanged);
        }

        private void ShapeChanged(UpdateShapeEvent evt)
        {
            Color shapeColor = m_SpaceshipShape.ShapeColor;

            if (m_SpaceshipShape != null)
                Destroy(m_SpaceshipShape.gameObject);

            m_SpaceshipShape = Instantiate(SpaceshipCustomizationSO.Instance.GetShapeByID(evt.ShapeID).ShapePrefab, transform);
            m_SpaceshipShape.Init();
            m_SpaceshipShape.SetEffect(m_Effect);
            m_SpaceshipShape.ShapeColor = shapeColor;
        }

        private void EffectChanged(UpdateEffectEvent evt)
        {
            var effect = SpaceshipCustomizationSO.Instance.GetEffectByID(evt.EffectID);
            m_Effect = effect.EffectType;
            m_SpaceshipShape.SetEffect(SpaceshipEffectType.None);
            m_SpaceshipShape.SetEffect(effect.EffectType);
        }

        private void TrailChanged(UpdateTrailEvent evt)
        {
            if (m_SpaceshipTrail != null)
                Destroy(m_SpaceshipTrail.gameObject);

            var trail = SpaceshipCustomizationSO.Instance.GetTrailByID(evt.TrailID);
            m_SpaceshipTrail = Instantiate(trail.TrailPrefab, transform);
            m_SpaceshipTrail.Init(trail.OverrideColor);

            Color color = SpaceshipTiers.Instance.GetTier(TierNumber).SpaceShipColor;
            m_SpaceshipTrail.SetColor(color);
        }

        private void OnDisable()
        {
            transform.DOKill();
        }

        private void OnDestroy()
        {
            transform.DOKill();

            EventManager<UpdateShapeEvent>.RemoveListener(ShapeChanged);
            EventManager<UpdateEffectEvent>.RemoveListener(EffectChanged);
            EventManager<UpdateTrailEvent>.RemoveListener(TrailChanged);
        }

        private void CreateShape(Color color, bool doColorTransition)
        {
            string shapeID = SaveLoadManager.Instance.GetSpaceshipShapeID();
            m_SpaceshipShape = Instantiate(SpaceshipCustomizationSO.Instance.GetShapeByID(shapeID).ShapePrefab, transform);
            m_SpaceshipShape.Init();
            if (!doColorTransition)
                m_SpaceshipShape.ShapeColor = color;
            else
                m_SpaceshipShape.DoMergeTint(color);

            SetupEffectColor(color);
        }

        private void SetupEffectColor(Color color)
        {
            var particleSystems = m_MergeEffect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
                if (col.enabled)
                    col.color = ApplyRgb(col.color, color);
            }
        }

        private ParticleSystem.MinMaxGradient ApplyRgb(ParticleSystem.MinMaxGradient mmg, Color rgb)
        {
            switch (mmg.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(WithRgb(mmg.color, rgb));

                case ParticleSystemGradientMode.Gradient:
                    Gradient g = mmg.gradient;
                    SetLastKey(g, rgb);
                    return new ParticleSystem.MinMaxGradient(g);

                case ParticleSystemGradientMode.RandomColor:
                    {
                        Gradient gMin = mmg.gradientMin;
                        Gradient gMax = mmg.gradientMax;
                        SetLastKey(gMin, rgb);
                        SetLastKey(gMax, rgb);
                        return new ParticleSystem.MinMaxGradient(gMin, gMax);
                    }

                default:
                    Debug.LogWarning("Gradient mode not support: " + mmg.mode);
                    return mmg;
            }
        }

        private void SetLastKey(Gradient g, Color rgb)
        {
            if (g == null) return;

            GradientColorKey[] keys = g.colorKeys;
            if (keys.Length == 0) return;

            GradientColorKey last = keys[keys.Length - 1];
            last.color = new Color(rgb.r, rgb.g, rgb.b, last.color.a);
            keys[keys.Length - 1] = last;

            g.colorKeys = keys;
        }

        private Color WithRgb(Color target, Color src)
        {
            target.r = src.r;
            target.g = src.g;
            target.b = src.b;
            return target;
        }

        private void CreateTrail(Color color)
        {
            string trailID = SaveLoadManager.Instance.GetSpaceshipTrailID();
            var trail = SpaceshipCustomizationSO.Instance.GetTrailByID(trailID);
            m_SpaceshipTrail = Instantiate(trail.TrailPrefab, transform);
            m_SpaceshipTrail.Init(trail.OverrideColor);
            m_SpaceshipTrail.SetColor(color);
        }

        public void ResetTrail()
        {
            m_SpaceshipTrail.ResetTrail();
        }

        public void DoShowFade(float startAlpha, float endAlpha, float fadeTime)
        {
            StartCoroutine(DoShowFadeCO(startAlpha, endAlpha, fadeTime));
        }

        private IEnumerator DoShowFadeCO(float startAlpha, float endAlpha, float fadeTime)
        {
            yield return new WaitUntil(() => m_SpaceshipShape != null && m_SpaceshipTrail != null);

            Color color = m_SpaceshipShape.ShapeColor;
            color.a = startAlpha;
            float time = 0f;

            while (time < fadeTime)
            {
                time += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, endAlpha, time / fadeTime);
                m_SpaceshipShape.ShapeColor = color;
                m_SpaceshipTrail.SetColor(color);
                yield return null;
            }
        }

        public void SetSpaceshipTier(SpaceshipTier tier, bool fromMerge = false)
        {
            TierNumber = tier.TierNumber;
            m_Coins = tier.Coins;

            if (m_SpaceshipShape == null)
                CreateShape(tier.SpaceShipColor, fromMerge);

            if (m_SpaceshipTrail == null)
                CreateTrail(tier.SpaceShipColor);

            if (fromMerge)
            {
                StartCoroutine(DoMergeEffect());
            }
        }

        private IEnumerator DoMergeEffect()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            var effectRenderer = m_MergeEffect.GetComponentsInChildren<ParticleSystemRenderer>();
            foreach (var renderer in effectRenderer)
                renderer.sortingLayerName = m_MergeSortingLayer;
            m_SpaceshipShape.SetSortingLayer(m_MergeSortingLayer);
            
            yield return new WaitForSeconds(0.1f);

            m_MergePunchTween = transform.DOPunchScale(Vector3.one * 3f, 0.5f, 1)
                    .OnUpdate(() =>
                    {
                        if (m_MergePunchTween.ElapsedPercentage() >= 0.3f)
                            m_MergeEffect.gameObject.SetActive(true);
                    })
                    .OnComplete(() =>
                    {
                        foreach (var renderer in effectRenderer)
                            renderer.sortingLayerName = m_DefaultSortingLayer;
                        m_SpaceshipShape.SetSortingLayer(m_DefaultSortingLayer);
                    });
        }

        public bool FinishedMerging()
        {
            return m_CanMerge && m_MergeEffect != null ? !m_MergeEffect.gameObject.activeInHierarchy : true;
        }

        public bool CanMerge()
        {
            bool isMaxTier = SpaceshipTiers.Instance.MaxTier == TierNumber;
            return !isMaxTier && m_CanMerge && m_MergeEffect != null ? !m_MergeEffect.gameObject.activeInHierarchy : true;
        }

        public void SetCanMerge(bool canMerge)
        {
            m_CanMerge = canMerge;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (this == null)
                return;

            if (gameObject == null) 
                return;

            if (collision == null) 
                return;

            if (collision.transform == null) 
                return;

            if (!collision.CompareTag(m_MoneyLineTag))
                return;

            if (!LevelManager.Instance.RewardLinesActive)
                return;

            RewardLine line = collision.GetComponent<RewardLine>();
            line.DoAction();

            EventManager<SpaceshipHitRewardLineEvent>.TriggerEvent(new SpaceshipHitRewardLineEvent { Spaceship = this, Line = line });

            int coins = Mathf.CeilToInt(m_Coins * LevelManager.Instance.MoneyMultiplier);

            if (line is PlanetHealRewardLine healLine)
                healLine.SpawnHealParticle(coins);
            else if (line is CombatRewardLine combatLine)
                combatLine.SpawnAttackParticle(m_SpaceshipShape.ShapeColor, coins);

            int economyPercentage = Debugger.ShipMoneyGenerationPercentage;
            coins *= LevelManager.Instance.DoubleCoinsActive ? 2 : 1;
            coins = Mathf.Max(Mathf.FloorToInt(coins * (1f + (economyPercentage / 100f))), 1);
            LevelManager.Instance.AddMoney(Mathf.Max(coins, 1));
            LevelManager.Instance.RewardLineCrossed();

            ShowCoinsText(coins);
        }

        private void ShowCoinsText(int coins)
        {
            var text = FloatingTextPooler.Instance.GetText();
            text.transform.position = transform.position;
            string coinsText = $"+${NumberFormatter.FormatValue(coins)}";
            text.SetText(coinsText);
            text.SetColor(m_SpaceshipShape.ShapeColor);
            text.gameObject.SetActive(true);
        }

        public void SetSortingLayer(int id)
        {
            m_SpaceshipShape.Renderer.sortingLayerID = id;
            m_SpaceshipTrail.SetSortingLayer(id);
        }

        public void SetSortingOrder(int order)
        {
            m_SpaceshipShape.Renderer.sortingOrder = order;
            m_SpaceshipTrail.SetSortingOrder(order - 1);
        }
    }
}
