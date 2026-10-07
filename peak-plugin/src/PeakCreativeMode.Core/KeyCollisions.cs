using System;
using System.Collections.Generic;

namespace PeakCreativeMode.Core
{
    public static class KeyCollisions
    {
        /// Returns one human-readable line per (our key, PEAK action) pair bound to the same control path.
        public static List<string> Find(IEnumerable<(string action, string path)> peakBindings,
                                        IEnumerable<(string name, string path)> ourKeys)
        {
            var result = new List<string>();
            var ours = new List<(string name, string path)>(ourKeys);
            foreach (var (action, path) in peakBindings)
            {
                if (string.IsNullOrEmpty(path)) continue;
                foreach (var (name, ourPath) in ours)
                {
                    if (string.Equals(path, ourPath, StringComparison.OrdinalIgnoreCase))
                        result.Add($"{name} ({ourPath}) is also bound to PEAK action '{action}'");
                }
            }
            return result;
        }
    }
}
