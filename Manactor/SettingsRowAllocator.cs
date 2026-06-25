using UnityEngine;

namespace ClassicUs.Manactor
{
    internal static class SettingsRowAllocator
    {
        private static int _lastFrame = -1;
        private static int _nextRow;

        public static int ReserveRows(int menuInstanceId, int count)
        {
            if (Time.frameCount != _lastFrame)
            {
                _lastFrame = Time.frameCount;
                _nextRow = 0;
            }

            var start = _nextRow;
            _nextRow += count;
            return start;
        }
    }
}
