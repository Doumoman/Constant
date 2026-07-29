using System;
using UnityEngine;

namespace StarFetchingNight
{
    public enum SunGrowthStage
    {
        Dormant,
        Awakened,
        Blooming,
        Overgrown,
        Burned
    }

    public enum SunGrowthKind
    {
        GardenPlant,
        PlatformPlant,
        SleepingCreature,
        StarPathTree,
        CoolingBloom
    }

    public readonly struct SunGrowthResult
    {
        public readonly SunGrowthStage previousStage;
        public readonly SunGrowthStage stage;
        public readonly int lightExposure;
        public readonly bool changed;
        public readonly bool overloaded;

        public SunGrowthResult(SunGrowthStage previous, SunGrowthStage current, int exposure)
        {
            previousStage = previous;
            stage = current;
            lightExposure = exposure;
            changed = previous != current;
            overloaded = current == SunGrowthStage.Overgrown || current == SunGrowthStage.Burned;
        }
    }

    [DisallowMultipleComponent]
    public sealed class SunGrowthState : MonoBehaviour
    {
        [SerializeField] private string growthId = "garden-growth";
        [SerializeField] private string displayName = "잠든 식물";
        [SerializeField] private SunGrowthKind kind = SunGrowthKind.GardenPlant;
        [SerializeField, Min(2)] private int bloomAt = 2;
        [SerializeField, Min(3)] private int burnAt = 5;
        [SerializeField] private SunGrowthStage stage;
        [SerializeField] private int lightExposure;
        [SerializeField] private GameObject controlledObject;
        [SerializeField] private GameObject barrierToDisable;

        private Vector3 baseScale;
        private SpriteRenderer sprite;

        public string GrowthId => growthId;
        public string DisplayName => displayName;
        public SunGrowthKind Kind => kind;
        public SunGrowthStage Stage => stage;
        public int LightExposure => lightExposure;
        public bool IsBurned => stage == SunGrowthStage.Burned;

        public event Action<SunGrowthState, SunGrowthStage, SunGrowthStage> StageChanged;
        public event Action<SunGrowthState, int> LightChanged;

        private void Awake()
        {
            baseScale = transform.localScale;
            sprite = GetComponent<SpriteRenderer>();
            ApplyVisualState();
        }

        public void Configure(string id, string label, SunGrowthKind growthKind,
            int requiredLight = 2, int burnThreshold = 5,
            GameObject controlled = null, GameObject barrier = null)
        {
            growthId = id;
            displayName = label;
            kind = growthKind;
            bloomAt = Mathf.Max(2, requiredLight);
            burnAt = Mathf.Max(bloomAt + 2, burnThreshold);
            controlledObject = controlled;
            barrierToDisable = barrier;
        }

        public SunGrowthResult ApplySunlight(int amount = 1)
        {
            SunGrowthStage previous = stage;
            lightExposure = Mathf.Max(0, lightExposure + Mathf.Max(1, amount));
            stage = ResolveStage(lightExposure);
            ApplyVisualState();
            LightChanged?.Invoke(this, lightExposure);
            if (previous != stage)
            {
                StageChanged?.Invoke(this, previous, stage);
            }
            return new SunGrowthResult(previous, stage, lightExposure);
        }

        public SunGrowthResult AdvanceNaturalGrowth()
        {
            if (kind == SunGrowthKind.StarPathTree || stage == SunGrowthStage.Burned)
            {
                return new SunGrowthResult(stage, stage, lightExposure);
            }
            return ApplySunlight();
        }

        public void SetStoryStage(SunGrowthStage value, int exposure = -1)
        {
            SunGrowthStage previous = stage;
            stage = value;
            if (exposure >= 0)
            {
                lightExposure = exposure;
            }
            ApplyVisualState();
            LightChanged?.Invoke(this, lightExposure);
            if (previous != stage)
            {
                StageChanged?.Invoke(this, previous, stage);
            }
        }

        private SunGrowthStage ResolveStage(int exposure)
        {
            if (exposure >= burnAt)
            {
                return SunGrowthStage.Burned;
            }
            if (exposure >= bloomAt + 1)
            {
                return SunGrowthStage.Overgrown;
            }
            if (exposure >= bloomAt)
            {
                return SunGrowthStage.Blooming;
            }
            return exposure > 0 ? SunGrowthStage.Awakened : SunGrowthStage.Dormant;
        }

        private void ApplyVisualState()
        {
            if (baseScale == Vector3.zero)
            {
                baseScale = transform.localScale;
            }
            if (sprite == null)
            {
                sprite = GetComponent<SpriteRenderer>();
            }

            float scale = stage switch
            {
                SunGrowthStage.Awakened => 1.18f,
                SunGrowthStage.Blooming => 1.55f,
                SunGrowthStage.Overgrown => 2.05f,
                SunGrowthStage.Burned => 0.82f,
                _ => 1f
            };
            transform.localScale = baseScale * scale;
            if (sprite != null)
            {
                sprite.color = stage switch
                {
                    SunGrowthStage.Awakened => new Color(1f, 0.88f, 0.35f),
                    SunGrowthStage.Blooming => new Color(0.55f, 1f, 0.48f),
                    SunGrowthStage.Overgrown => new Color(0.25f, 0.82f, 0.32f),
                    SunGrowthStage.Burned => new Color(0.25f, 0.14f, 0.12f),
                    _ => new Color(0.42f, 0.48f, 0.52f)
                };
            }

            bool usefulGrowth = stage == SunGrowthStage.Awakened ||
                                stage == SunGrowthStage.Blooming ||
                                stage == SunGrowthStage.Overgrown;
            if (controlledObject != null)
            {
                controlledObject.SetActive(usefulGrowth);
            }
            if (barrierToDisable != null)
            {
                barrierToDisable.SetActive(stage < SunGrowthStage.Blooming);
            }
        }
    }
}
