#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Tools
{
    [DisallowMultipleComponent]
    public sealed class ToolChargeDots2D : MonoBehaviour
    {
        [SerializeField] private HandToolPickup2D observedTool;
        [SerializeField] private Sprite dotSprite;
        [SerializeField] private Color availableColor =
            new Color(1f, 0.88f, 0.30f, 1f);
        [SerializeField] private Color spentColor =
            new Color(0.20f, 0.24f, 0.32f, 0.72f);
        [SerializeField, Min(0.02f)] private float spacing = 0.16f;
        [SerializeField, Min(0.02f)] private float scale = 0.10f;

        private readonly List<SpriteRenderer> dots = new List<SpriteRenderer>();

        public void Configure(
            HandToolPickup2D tool,
            Sprite chargeDotSprite,
            Color active,
            Color inactive)
        {
            Unsubscribe();
            observedTool = tool;
            dotSprite = chargeDotSprite;
            availableColor = active;
            spentColor = inactive;
            Rebuild();
            Subscribe();
        }

        private void OnEnable()
        {
            Rebuild();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (observedTool != null)
            {
                observedTool.UsesChanged -= OnUsesChanged;
                observedTool.UsesChanged += OnUsesChanged;
            }
        }

        private void Unsubscribe()
        {
            if (observedTool != null)
            {
                observedTool.UsesChanged -= OnUsesChanged;
            }
        }

        private void Rebuild()
        {
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                Transform child = transform.GetChild(index);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            dots.Clear();
            if (observedTool == null || !observedTool.HasFiniteUses)
            {
                return;
            }

            float width = (observedTool.MaximumUses - 1) * spacing;
            for (int index = 0; index < observedTool.MaximumUses; index++)
            {
                GameObject dot = new GameObject($"ChargeDot_{index + 1:00}");
                dot.transform.SetParent(transform, false);
                dot.transform.localPosition =
                    new Vector3(index * spacing - width * 0.5f, 0f, 0f);
                dot.transform.localScale = Vector3.one * scale;
                SpriteRenderer renderer = dot.AddComponent<SpriteRenderer>();
                renderer.sprite = dotSprite;
                renderer.sortingOrder = 500;
                dots.Add(renderer);
            }

            Refresh(observedTool.RemainingUses);
        }

        private void OnUsesChanged(HandToolPickup2D tool, int remaining)
        {
            Refresh(remaining);
        }

        private void Refresh(int remaining)
        {
            for (int index = 0; index < dots.Count; index++)
            {
                dots[index].color = index < remaining
                    ? availableColor
                    : spentColor;
            }
        }
    }
}

#endif
