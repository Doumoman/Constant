#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Narrative
{
    [Serializable]
    public sealed class CharacterPresentation
    {
        public string characterId;
        public string nameKey;
        public string displayName;
        public Sprite portrait;
        public Color bubbleColor = Color.white;
        public string textSoundId;
    }

    [CreateAssetMenu(menuName = "Star Night/Narrative/Character Database", fileName = "CharacterDatabase")]
    public sealed class CharacterDatabase : ScriptableObject
    {
        [SerializeField] private CharacterPresentation[] characters = Array.Empty<CharacterPresentation>();

        public CharacterPresentation[] Characters => characters;

        public void Configure(CharacterPresentation[] entries)
        {
            characters = entries ?? Array.Empty<CharacterPresentation>();
        }

        public bool TryGet(string characterId, out CharacterPresentation presentation)
        {
            for (int index = 0; index < characters.Length; index++)
            {
                CharacterPresentation candidate = characters[index];
                if (candidate != null && string.Equals(candidate.characterId, characterId, StringComparison.Ordinal))
                {
                    presentation = candidate;
                    return true;
                }
            }

            presentation = null;
            return false;
        }

        public string ResolveDisplayName(string characterId)
        {
            return TryGet(characterId, out CharacterPresentation presentation)
                ? presentation.displayName
                : characterId ?? string.Empty;
        }
    }
}

#endif
