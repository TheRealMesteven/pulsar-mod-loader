﻿using System;
using System.Collections.Generic;
using System.Reflection;
using PulsarModLoader.Utilities;
using HarmonyLib;
using CodeStage.AntiCheat.ObscuredTypes;
using static PulsarModLoader.Patches.HarmonyHelpers;
using static HarmonyLib.AccessTools;
using System.Reflection.Emit;

namespace PulsarModLoader.Content.Talents
{
    public class TalentModManager
    {
        public readonly int vanillaTalentMaxType = 0;
        public readonly int moddedTalentMaxType = 0;
        private static TalentModManager m_instance = null;
        public readonly List<TalentMod> TalentTypes = new List<TalentMod>();
        public static TalentModManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new TalentModManager();
                }
                return m_instance;
            }
        }

        TalentModManager()
        {
            vanillaTalentMaxType = Enum.GetValues(typeof(ETalents)).Length;
            Logger.Info($"MaxTalentId = {vanillaTalentMaxType - 1}");
            foreach (PulsarMod mod in ModManager.Instance.GetAllMods())
            {
                Assembly asm = mod.GetType().Assembly;
                Type talentMod = typeof(TalentMod);
                foreach (Type t in asm.GetTypes())
                {
                    if (talentMod.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    {
                        TalentMod talentModHandler = (TalentMod)Activator.CreateInstance(t);
                        if (GetTalentIDFromName(talentModHandler.Name) == -1)
                        {
                            TalentTypes.Add(talentModHandler);
                            Logger.Info($"Added Talent: '{talentModHandler.Name}' with ID '{GetTalentIDFromName(talentModHandler.Name)}'");
                        }
                        else
                        {
                            Logger.Info($"Could not add Talent from {mod.Name} with the duplicate name of '{talentModHandler.Name}'");
                        }
                    }
                }
            }
            moddedTalentMaxType = vanillaTalentMaxType + TalentTypes.Count;
            Logger.Info($"ModdedMaxTalentId = {moddedTalentMaxType - 1}");
        }

        /// <summary>
        /// Finds Talent ID equivilent to given name. Returns -1 if couldn't find Talent.
        /// </summary>
        /// <param name="TalentName">Name of Talent</param>
        /// <returns>ID of Talent</returns>
        public int GetTalentIDFromName(string TalentName)
        {
            for (int i = 0; i < TalentTypes.Count; i++)
            {
                if (TalentTypes[i].Name == TalentName)
                {
                    return i + vanillaTalentMaxType;
                }
            }
            return -1;
        }
    }

    // Makes new Talent Infos retrievable
    [HarmonyPatch(typeof(PLGlobal), "GetTalentInfoForTalentType")]
    class TalentInfoFix
    {
        static bool Prefix(ETalents inTalent, ref TalentInfo __result)
        {
            int subtypeformodded = (int)inTalent - TalentModManager.Instance.vanillaTalentMaxType;
            if (subtypeformodded <= TalentModManager.Instance.TalentTypes.Count && subtypeformodded > -1)
            {
                __result = TalentModManager.Instance.TalentTypes[subtypeformodded].TalentInfo;
                return false;
            }
            return true;
        }
    }

    // Increases the array size of Talents (Sometimes it isnt caught by Start Patch)
    [HarmonyPatch(typeof(PLPlayer), "Update")]
    class EnsureTalentSizePatch
    {
        static void Prefix(PLPlayer __instance)
        {
            if (__instance == null || __instance.Talents == null) return;
            int TalentMaxSize = TalentModManager.Instance.moddedTalentMaxType;
            if (__instance.Talents.Length <  TalentMaxSize)
            {
                __instance.Talents = new ObscuredInt[TalentMaxSize];
                __instance.TalentsLocalEditTime = new float[TalentMaxSize];
            }
        }
    }

    // Allows new Talents to be researchable
    [HarmonyPatch(typeof(PLShipInfo), "UpdateResearchTalentChoices")]
    class ResearchTalentSizePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return PatchBySequence(instructions,
            new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4_S, 0x3F),
            },
            new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Call, Method(typeof(ResearchTalentSizePatch), "Patch"))
            },
            PatchMode.REPLACE, showDebugOutput: true);
        }
        static int Patch()
        {
            PulsarModLoader.Utilities.Logger.Info("ResearchTalentSizePatch");
            return TalentModManager.Instance.moddedTalentMaxType;
        }
    }

    // Allows new Talents to be synced with joining players
    [HarmonyPatch(typeof(PLPlayer), "SendTalentsToPhotonTargets")]
    class TalentSerializePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return PatchBySequence(instructions,
            new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4_S, 0x3F),
            },
            new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Call, Method(typeof(TalentSerializePatch), "Patch"))
            },
            PatchMode.REPLACE, showDebugOutput: true);
        }
        static int Patch()
        {
            PulsarModLoader.Utilities.Logger.Info("TalentSerializePatch");
            return TalentModManager.Instance.moddedTalentMaxType;
        }
    }
}
