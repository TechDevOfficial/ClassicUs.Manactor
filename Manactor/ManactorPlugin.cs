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
        public const string Version = "1.2.0";

        public static ManualLogSource Log;

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("Applying Manactor Harmony patches.");
            new Harmony(Guid).PatchAll();
            Log.LogInfo("Manactor Harmony patches applied.");
            ManactorAPI.RegisterRpcMethods(typeof(CustomKillManager));
            Log.LogInfo("Manactor RPC methods registered.");
            Log.LogInfo("Manactor loaded.");
        }
    }
}
