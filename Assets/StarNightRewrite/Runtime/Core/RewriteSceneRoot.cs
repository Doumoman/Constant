using UnityEngine;

namespace StarNight.Rewrite.Core
{
    [DisallowMultipleComponent]
    public sealed class RewriteSceneRoot : MonoBehaviour
    {
        public const string ContractVersion = "RW0-v1";

        [SerializeField]
        private string milestone = "RW0";

        public string Milestone => milestone;
    }
}
