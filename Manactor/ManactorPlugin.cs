using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ClassicUs.Manactor
{
    [BepInPlugin(Guid, "Manactor", Version)]
    public class ManactorPlugin : BasePlugin
    {
        public const string Guid = "classicus.manactor";
        public const string Version = "1.0.4";

        public static ManualLogSource Log;

        public override void Load()
        {
            Log = base.Log;
            new Harmony(Guid).PatchAll();
            Log.LogInfo("Manactor loaded.");
        }
    }
}
