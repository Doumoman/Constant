#if LEGACY_DISABLED
namespace StarNight.Stage.Secrets
{
    public static class SecretSeedUtility
    {
        public static int Create(int stageSeed, string sourceRoomStableId, string anchorStableId)
        {
            unchecked
            {
                int hash = stageSeed == 0 ? 17 : stageSeed;
                hash = Append(hash, sourceRoomStableId);
                hash = Append(hash, anchorStableId);
                return Append(hash, "SECRET");
            }
        }

        private static int Append(int hash, string value)
        {
            for (int index = 0; index < (value?.Length ?? 0); index++)
            {
                hash = hash * 31 + value[index];
            }
            return hash;
        }
    }
}

#endif
