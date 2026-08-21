#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P11
{
    public enum P11ParcelLabel
    {
        None = 0,
        Moon = 1,
        Bird = 2,
        Shell = 3,
        Sun = 4,
        Star = 5
    }

    [DisallowMultipleComponent]
    public sealed class P11AddressableParcel2D : MonoBehaviour
    {
        [SerializeField] private P11ParcelLabel label;
        [SerializeField] private SpriteRenderer labelVisual;
        [SerializeField] private int labelChangeCount;
        [SerializeField] private P11ParcelLabel previousLabel;

        public P11ParcelLabel Label => label;
        public P11ParcelLabel PreviousLabel => previousLabel;
        public int LabelChangeCount => labelChangeCount;
        public SpriteRenderer LabelVisual => labelVisual;
        public Rigidbody2D Body => GetComponent<Rigidbody2D>();

        public void Configure(
            P11ParcelLabel addressLabel,
            SpriteRenderer visual = null)
        {
            label = addressLabel;
            previousLabel = addressLabel;
            labelChangeCount = 0;
            labelVisual = visual;
            RefreshVisual();
        }

        public bool ApplyLabel(P11ParcelLabel next)
        {
            if (next == label)
            {
                return false;
            }

            previousLabel = label;
            label = next;
            labelChangeCount++;
            RefreshVisual();
            return true;
        }

        private void RefreshVisual()
        {
            if (labelVisual != null)
            {
                labelVisual.color = LabelColor(label);
            }
        }

        public static Color LabelColor(P11ParcelLabel value)
        {
            switch (value)
            {
                case P11ParcelLabel.Moon:
                    return new Color(0.72f, 0.78f, 1f, 1f);
                case P11ParcelLabel.Bird:
                    return new Color(0.88f, 0.65f, 1f, 1f);
                case P11ParcelLabel.Shell:
                    return new Color(0.35f, 0.92f, 0.90f, 1f);
                case P11ParcelLabel.Sun:
                    return new Color(1f, 0.68f, 0.20f, 1f);
                case P11ParcelLabel.Star:
                    return new Color(1f, 0.92f, 0.42f, 1f);
                default:
                    return Color.gray;
            }
        }
    }
}

#endif
