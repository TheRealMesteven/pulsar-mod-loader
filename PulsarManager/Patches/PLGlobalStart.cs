using HarmonyLib;
using System.IO;
using UnityEngine;

namespace PulsarManager.Patches
{
    [HarmonyPatch(typeof(PLGlobal), "Start")]
    class PLGlobalStart
    {
        private static bool modsLoaded = false;

        static void Prefix()
        {
            if (!modsLoaded)
            {
                //Events Init
                new PulsarManager.Events();

                //Modmanager GUI Init.
                new GameObject("ModManager", typeof(CustomGUI.GUIMain)) { hideFlags = HideFlags.HideAndDontSave };

                //SaveDataManager Init()
                new SaveData.SaveDataManager();

                //KeybindManager Init()
                _ = PulsarManager.Keybinds.KeybindManager.Instance;

                //MP Mod Checks
                new MPModChecks.MPModCheckManager();

                //ModLoading
                ModManager.Instance.LoadModsDirectory(ModManager.GetModsDir());

                modsLoaded = true;
            }
        }
    }
}
