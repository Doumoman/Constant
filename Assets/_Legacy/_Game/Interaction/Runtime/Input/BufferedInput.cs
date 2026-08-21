#if LEGACY_DISABLED
namespace StarNight.Interaction.Input
{
    public struct BufferedInput
    {
        private float expiresAt;
        private bool buffered;

        public bool IsBuffered(float now)
        {
            return buffered && now <= expiresAt;
        }

        public void Buffer(float now, float duration)
        {
            buffered = true;
            expiresAt = now + duration;
        }

        public bool TryConsume(float now)
        {
            if (!IsBuffered(now))
            {
                buffered = false;
                return false;
            }

            buffered = false;
            return true;
        }

        public void Clear()
        {
            buffered = false;
            expiresAt = 0f;
        }
    }
}

#endif
