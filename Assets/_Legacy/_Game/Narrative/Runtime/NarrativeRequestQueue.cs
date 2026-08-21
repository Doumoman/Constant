#if LEGACY_DISABLED
using System;
using System.Collections.Generic;

namespace StarNight.Narrative
{
    public sealed class NarrativeRequestQueue
    {
        private const int MaxQueuedFieldLines = 1;
        private readonly List<NarrativeRequest> requests = new();

        public int Count => requests.Count;

        public bool Contains(string nodeName)
        {
            for (int index = 0; index < requests.Count; index++)
            {
                if (string.Equals(requests[index].NodeName, nodeName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Enqueue(NarrativeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NodeName) || Contains(request.NodeName))
            {
                return false;
            }

            if (request.Mode == NarrativeMode.Bubble)
            {
                int fieldCount = 0;
                for (int index = 0; index < requests.Count; index++)
                {
                    if (requests[index].Mode == NarrativeMode.Bubble)
                    {
                        fieldCount++;
                    }
                }

                if (fieldCount >= MaxQueuedFieldLines)
                {
                    int discardIndex = requests.FindIndex(item => item.Mode == NarrativeMode.Bubble && !item.Essential);
                    if (discardIndex >= 0)
                    {
                        requests.RemoveAt(discardIndex);
                    }
                    else if (!request.Essential)
                    {
                        return false;
                    }
                }
            }

            requests.Add(request);
            return true;
        }

        public bool TryDequeue(out NarrativeRequest request)
        {
            if (requests.Count == 0)
            {
                request = default;
                return false;
            }

            request = requests[0];
            requests.RemoveAt(0);
            return true;
        }

        public void Clear() => requests.Clear();

        public void RemoveMode(NarrativeMode mode)
        {
            requests.RemoveAll(request => request.Mode == mode);
        }
    }
}

#endif
