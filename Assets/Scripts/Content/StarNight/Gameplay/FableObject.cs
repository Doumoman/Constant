using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class FableObject : MonoBehaviour
    {
        [SerializeField] private string objectId;
        [SerializeField] private string displayName = "이름 없는 물건";
        [SerializeField] private StarItemKind itemKind;
        [SerializeField] private FableTraits traits = FableTraits.Carryable | FableTraits.Resizable;
        [SerializeField] private float scentWeight = 1f;
        [SerializeField] private int maximumModifications = 3;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color overloadedColor = new(1f, 0.22f, 0.45f);

        private readonly List<FableModification> modifications = new();
        private Vector3 baseScale;
        private float baseGravity;
        private Rigidbody2D body;
        private SpriteRenderer sprite;
        private Collider2D hitbox;
        private bool stored;

        public string ObjectId => string.IsNullOrWhiteSpace(objectId) ? gameObject.name : objectId;
        public string DisplayName => displayName;
        public StarItemKind ItemKind => itemKind;
        public FableTraits Traits => traits;
        public float ScentWeight => scentWeight;
        public int ModificationCount => modifications.Count;
        public int MaximumModifications => maximumModifications;
        public bool IsOverloaded => modifications.Contains(FableModification.Overloaded);
        public bool IsLinked => modifications.Contains(FableModification.Linked);
        public bool IsStored => stored;
        public Rigidbody2D Body => body != null ? body : body = GetComponent<Rigidbody2D>();
        public float EffectiveMass => Body != null ? Mathf.Max(0.01f, Body.mass) : float.PositiveInfinity;
        public IReadOnlyList<FableModification> Modifications => modifications;
        public event Action<FableObject, FableModification> Modified;
        public event Action<FableObject> Overloaded;

        private void Awake()
        {
            baseScale = transform.localScale;
            body = GetComponent<Rigidbody2D>();
            sprite = GetComponent<SpriteRenderer>();
            hitbox = GetComponent<Collider2D>();
            if (body != null)
            {
                baseGravity = body.gravityScale;
            }
        }

        public void Configure(string id, string label, StarItemKind kind, FableTraits objectTraits, float scent = 1f)
        {
            objectId = id;
            displayName = label;
            itemKind = kind;
            traits = objectTraits;
            scentWeight = scent;
        }

        public bool HasTrait(FableTraits trait) => (traits & trait) == trait;

        public bool Accepts(FableVerb verb)
        {
            if (IsOverloaded)
            {
                return false;
            }

            return verb switch
            {
                FableVerb.Resize => HasTrait(FableTraits.Resizable),
                FableVerb.Link => HasTrait(FableTraits.Linkable),
                FableVerb.Float => HasTrait(FableTraits.Floatable),
                FableVerb.Deliver => HasTrait(FableTraits.Deliverable) ||
                                     HasTrait(FableTraits.PostalParcel) ||
                                     HasTrait(FableTraits.PostalAddress),
                FableVerb.Awaken => HasTrait(FableTraits.LightReactive) || HasTrait(FableTraits.Living),
                _ => false
            };
        }

        public FableToolResult Apply(FableVerb verb, ResizeIntent resizeIntent)
        {
            if (verb == FableVerb.Link)
            {
                return ApplyLinkState(true);
            }

            if (!Accepts(verb))
            {
                return FableToolResult.Fail(IsOverloaded
                    ? $"{displayName}은 이미 너무 많은 이야기를 품고 있다."
                    : $"{displayName}에는 이 도구의 말이 통하지 않는다.");
            }

            if (modifications.Count >= maximumModifications)
            {
                return TriggerOverload();
            }

            FableModification modification = verb switch
            {
                FableVerb.Resize => resizeIntent == ResizeIntent.Enlarge ? FableModification.Large : FableModification.Small,
                FableVerb.Link => FableModification.Linked,
                FableVerb.Float => FableModification.Floating,
                FableVerb.Deliver => FableModification.DeliveryPending,
                FableVerb.Awaken => FableModification.Awakened,
                _ => FableModification.Overloaded
            };

            if ((modification == FableModification.Large && modifications.Contains(FableModification.Small)) ||
                (modification == FableModification.Small && modifications.Contains(FableModification.Large)))
            {
                modifications.Remove(FableModification.Large);
                modifications.Remove(FableModification.Small);
                transform.localScale = baseScale;
            }
            else
            {
                modifications.Add(modification);
                ApplyPhysicalState(modification);
            }

            Modified?.Invoke(this, modification);
            float scent = ScentFor(verb);
            return new FableToolResult
            {
                success = true,
                sentence = SentenceFor(modification),
                scentAdded = scent
            };
        }

        public FableToolResult ApplyLinkState(bool linked)
        {
            if (!linked)
            {
                modifications.Remove(FableModification.Linked);
                return new FableToolResult
                {
                    success = true,
                    connectionChanged = true,
                    sentence = $"{displayName}에서 붉은 실이 풀렸다."
                };
            }

            if (!HasTrait(FableTraits.Linkable))
            {
                return FableToolResult.Fail($"{displayName}에는 붉은 실을 걸 곳이 없다.");
            }
            if (IsOverloaded)
            {
                return FableToolResult.Fail($"{displayName}은 이미 너무 많은 이야기를 품고 있다.");
            }
            if (IsLinked)
            {
                return new FableToolResult
                {
                    success = true,
                    sentence = $"{displayName}이 다른 붉은 실 끝을 기다린다."
                };
            }
            if (modifications.Count >= maximumModifications)
            {
                return TriggerOverload();
            }

            modifications.Add(FableModification.Linked);
            Modified?.Invoke(this, FableModification.Linked);
            return new FableToolResult
            {
                success = true,
                connectionChanged = true,
                sentence = SentenceFor(FableModification.Linked),
                scentAdded = ScentFor(FableVerb.Link)
            };
        }

        private void ApplyPhysicalState(FableModification modification)
        {
            switch (modification)
            {
                case FableModification.Large:
                    transform.localScale = baseScale * 1.8f;
                    if (body != null) body.mass *= 2f;
                    break;
                case FableModification.Small:
                    transform.localScale = baseScale * 0.58f;
                    if (body != null) body.mass = Mathf.Max(0.1f, body.mass * 0.5f);
                    break;
                case FableModification.Floating:
                    if (body != null) body.gravityScale = -0.12f;
                    break;
                case FableModification.Awakened:
                    if (sprite != null) sprite.color = new Color(1f, 0.9f, 0.35f);
                    break;
                case FableModification.DeliveryPending:
                    if (sprite != null) sprite.color = new Color(0.4f, 0.95f, 1f);
                    break;
            }
        }

        private FableToolResult TriggerOverload()
        {
            modifications.Add(FableModification.Overloaded);
            if (sprite != null) sprite.color = overloadedColor;
            if (body != null)
            {
                body.gravityScale = Mathf.Max(0.5f, baseGravity);
                body.AddForce(Vector2.up * 6f + UnityEngine.Random.insideUnitCircle * 2f, ForceMode2D.Impulse);
            }

            Overloaded?.Invoke(this);
            return new FableToolResult
            {
                success = true,
                overloaded = true,
                sentence = $"{displayName}에 네 번째 말이 겹쳤다. 이야기가 터져 나온다!",
                scentAdded = 18f
            };
        }

        public void SetStored(bool value)
        {
            stored = value;
            if (sprite != null) sprite.enabled = !value;
            if (hitbox != null) hitbox.enabled = !value;
            if (body != null)
            {
                body.simulated = !value;
                if (!value) body.linearVelocity = Vector2.zero;
            }
        }

        public void RestoreVisual()
        {
            if (sprite != null) sprite.color = normalColor;
        }

        private float ScentFor(FableVerb verb)
        {
            float baseValue = verb switch
            {
                FableVerb.Resize => 5f,
                FableVerb.Link => 7f,
                FableVerb.Float => 8f,
                FableVerb.Deliver => 4f,
                FableVerb.Awaken => 10f,
                _ => 5f
            };
            return baseValue * Mathf.Max(0.25f, scentWeight);
        }

        private string SentenceFor(FableModification modification) => modification switch
        {
            FableModification.Large => $"{displayName}이 방보다 큰 욕심을 품었다.",
            FableModification.Small => $"{displayName}이 주머니만 한 비밀이 되었다.",
            FableModification.Linked => $"{displayName}이 보이지 않는 붉은 실을 붙잡았다.",
            FableModification.Floating => $"{displayName}이 무게를 잠시 잊었다.",
            FableModification.DeliveryPending => $"{displayName}이 다른 곳으로 갈 주소를 얻었다.",
            FableModification.Awakened => $"{displayName} 안의 잠든 빛이 눈을 떴다.",
            _ => $"{displayName}에 변화가 생겼다."
        };
    }
}
