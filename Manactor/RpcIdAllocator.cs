using System;
using System.Collections.Generic;

namespace ClassicUs.Manactor
{
    internal static class RpcIdAllocator
    {
        private const int RangeStart = 212;
        private const int RangeEnd = 250;

        private static readonly List<string> _pendingKeys = new();
        private static Dictionary<string, byte> _finalized;

        public static void Reserve(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_finalized != null)
            {
                ManactorPlugin.Log.LogError($"[RpcIdAllocator] '{key}' reserved after ids were already finalized; it may not match other peers.");
                return;
            }
            if (!_pendingKeys.Contains(key)) _pendingKeys.Add(key);
        }

        public static byte GetId(string key)
        {
            if (_finalized == null) FinalizeIds();
            if (_finalized.TryGetValue(key, out var id)) return id;

            ManactorPlugin.Log.LogError($"[RpcIdAllocator] '{key}' was never reserved.");
            return 0;
        }

        private static void FinalizeIds()
        {
            var sortedKeys = new List<string>(_pendingKeys);
            sortedKeys.Sort(StringComparer.Ordinal);

            _finalized = new Dictionary<string, byte>();
            int next = RangeStart;
            foreach (var key in sortedKeys)
            {
                if (next > RangeEnd)
                {
                    ManactorPlugin.Log.LogError($"[RpcIdAllocator] Ran out of RPC id space, '{key}' was not assigned.");
                    continue;
                }
                _finalized[key] = (byte)next;
                next++;
            }

            ManactorPlugin.Log.LogInfo("[RpcIdAllocator] Finalized RPC ids: " + string.Join(", ", _finalized.Keys));
        }
    }
}
