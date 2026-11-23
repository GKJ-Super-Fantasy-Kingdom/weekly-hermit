using System;
using System.Collections.Generic;

namespace WeeklyHermit
{
    internal static class RecruitmentPool
    {
        // The merchant draws a prefab but later builds and grants the card by its
        // entity identifier. A replacement must resolve through both lookups.
        internal static List<T> Read<T>(IEnumerable<string> unlocked, IEnumerable<string> banned,
            Func<string, T> lookup, Func<T, string> identifier, Func<string, bool> hermitAllows,
            Func<T, bool> merchantAllows) where T : class
        {
            var excluded = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in banned)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                excluded.Add(key);
                var entry = lookup(key);
                if (entry != null) excluded.Add(identifier(entry));
            }
            var offered = new HashSet<string>(StringComparer.Ordinal);
            var units = new List<T>();
            foreach (string key in unlocked)
            {
                if (string.IsNullOrWhiteSpace(key) || excluded.Contains(key))
                    continue;

                var candidate = lookup(key);
                if (candidate == null)
                    continue;

                string id = identifier(candidate);
                if (string.IsNullOrWhiteSpace(id) || excluded.Contains(id) || offered.Contains(id))
                    continue;

                if (id != key)
                    candidate = lookup(id);
                if (candidate == null || identifier(candidate) != id || !hermitAllows(key)
                    || (id != key && !hermitAllows(id)) || !merchantAllows(candidate))
                    continue;

                offered.Add(id);
                units.Add(candidate);
            }
            // The saved seed must deal the same cards regardless of registration order.
            units.Sort((left, right) => StringComparer.Ordinal.Compare(identifier(left), identifier(right)));
            return units;
        }
    }

    internal sealed class MapHandoff
    {
        private int clearFrames;

        internal bool CanOpen(bool mapAvailable, bool guestActive, bool overlayOpen, bool overlayMinimized)
        {
            if (!mapAvailable || guestActive || overlayOpen || overlayMinimized)
                clearFrames = 0;
            else
                clearFrames++;
            return clearFrames >= 2;
        }
    }
}
