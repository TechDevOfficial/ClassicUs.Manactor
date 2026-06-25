using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClassicUs.Manactor
{
    internal static class Il2CppTypeRegistrar
    {
        private const int FramesBetweenRegistrations = 15;

        private static readonly Queue<Action> _pending = new();
        private static int _lastServicedFrame = -1;

        public static void Enqueue(Action register) => _pending.Enqueue(register);

        public static void Tick()
        {
            if (_pending.Count == 0) return;
            if (_lastServicedFrame >= 0 && Time.frameCount - _lastServicedFrame < FramesBetweenRegistrations) return;
            _lastServicedFrame = Time.frameCount;

            var register = _pending.Dequeue();
            try { register(); }
            catch (Exception e) { ManactorPlugin.Log.LogError("Il2CppTypeRegistrar: " + e); }
        }
    }
}
