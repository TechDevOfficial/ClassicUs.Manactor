using System;
using Il2CppInterop.Runtime.InteropTypes;

namespace ClassicUs.Manactor
{
    public static class Il2CppSafeCast
    {
        public static T SafeTryCast<T>(this Il2CppObjectBase obj) where T : Il2CppObjectBase
        {
            if (obj == null) return null;
            try { return obj.TryCast<T>(); }
            catch (ArgumentException) { return null; }
        }
    }
}
