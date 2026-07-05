using System;

namespace ClassicUs.Manactor
{
    internal static class Il2CppTypeRegistrar
    {
        public static void Enqueue(Action register)
        {
            try { register(); }
            catch (Exception e) { ManactorPlugin.Log.LogError("Il2CppTypeRegistrar: " + e); }
        }

        public static void Tick() { }

        public static void FlushAll() { }
    }
}
