using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System;


namespace PulsarManager
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.USERS_PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("PULSAR_LostColony.exe")]
    public class BepinPlugin : BaseUnityPlugin
    {
        internal static BepinPlugin instance;
        internal static readonly Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        internal static ManualLogSource Log;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "N/A")]
        private void Awake()
        {
            instance = this;
            Log = Logger;

            //Wrapped with try catch. If PatchAll fails, further code does not run.
            try { Harmony.PatchAll(); }
            catch (Exception e) { Log.LogError(e); }

            //Events Init
            //new PulsarModLoader.Events();

            //Modmanager GUI Init.
            //new GameObject("ModManager", typeof(CustomGUI.GUIMain)) { hideFlags = HideFlags.HideAndDontSave };

            //SaveDataManager Init()
            //new SaveData.SaveDataManager();

            //KeybindManager Init()
            //_ = PulsarModLoader.Keybinds.KeybindManager.Instance;

            //MP Mod Checks
            //new MPModChecks.MPModCheckManager();

            //ModLoading
            //ModManager.Instance.LoadModsDirectory(ModManager.GetModsDir());

            Log.LogInfo($"{MyPluginInfo.PLUGIN_GUID} Initialized.");
        }
    }
}
