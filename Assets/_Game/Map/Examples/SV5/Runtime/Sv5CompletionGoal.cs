using UnityEngine;

namespace StarNight.Map.SV5.Examples
{
    /// <summary>
    /// The example map has one completion condition: reach the SV5 Exit.
    /// It deliberately owns no auxiliary progression state.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Sv5CompletionGoal : MonoBehaviour
    {
        [SerializeField] private TextMesh statusText;
        [SerializeField] private bool completed;

        public bool Completed => completed;

        public void Configure(TextMesh targetStatusText)
        {
            statusText = targetStatusText;
            completed = false;
            RefreshText();
        }

        public void ResetGoal()
        {
            completed = false;
            RefreshText();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<Sv5ExamplePlayerController>() == null)
            {
                return;
            }

            completed = true;
            RefreshText();
        }

        private void RefreshText()
        {
            if (statusText != null)
            {
                statusText.text = completed
                    ? "SV5 COMPLETION: PASS"
                    : "SV5 COMPLETION: START -> EXIT";
                statusText.color = completed
                    ? new Color(0.35f, 1f, 0.62f, 1f)
                    : new Color(0.88f, 0.96f, 1f, 1f);
            }
        }
    }
}
