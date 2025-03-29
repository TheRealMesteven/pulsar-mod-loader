using HarmonyLib;
using PulsarModLoader.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static PulsarModLoader.Patches.HarmonyHelpers;

namespace PulsarModLoader.Content.Talents
{
    // Change what Talents are available for a player to use.
    [HarmonyPatch(typeof(PLTabMenu), "UpdateTDs")]
    class TalentTreesPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> target = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldloc_S),       // playerFromPlayerID
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PLGlobal), "TalentsForClass", new Type[] { typeof(Int32) })),
            };
            int NextInstruction = FindSequence(instructions, target, CheckMode.NONNULL);
            List<CodeInstruction> patch = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0),       // PLTabMenu Instance
                instructions.ToList()[NextInstruction - 3], // playerFromPlayerID
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TalentTreesPatch), "Patch", new Type[] { typeof(PLTabMenu), typeof(PLPlayer) }))
            };
            return PatchBySequence(instructions, target, patch, PatchMode.REPLACE, CheckMode.NONNULL);
        }
        public static List<ETalents> Patch(PLTabMenu instance, PLPlayer pLPlayer) => TalentTreeImplementation.TalentsForClassSpecies(pLPlayer, pLPlayer.GetClassID());
    }
    public class TalentTreeModManager
    {
        private static TalentTreeModManager m_instance = null;
        public readonly Dictionary<ETalents,TalentTreeMod> TalentOverrides = new Dictionary<ETalents, TalentTreeMod>();
        public static TalentTreeModManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new TalentTreeModManager();
                }
                return m_instance;
            }
        }

        TalentTreeModManager()
        {
            foreach (PulsarMod mod in ModManager.Instance.GetAllMods())
            {
                Assembly asm = mod.GetType().Assembly;
                Type talentMod = typeof(TalentTreeMod);
                foreach (Type t in asm.GetTypes())
                {
                    if (talentMod.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    {
                        TalentTreeMod talentOverride = (TalentTreeMod)Activator.CreateInstance(t);
                        if (!TalentOverrides.ContainsKey(talentOverride.Talent))
                        {
                            TalentOverrides.Add(talentOverride.Talent, talentOverride);
                            Logger.Info($"Added Talent Override: '{talentOverride.Talent.ToString()}'");
                        }
                        else
                        {
                            Logger.Info($"Could not add Talent Override from {mod.Name} with the duplicate override of '{talentOverride.Talent.ToString()}'");
                        }
                    }
                }
            }
        }
    }
    internal class TalentTreeImplementation
    {
        public static Dictionary<int, Dictionary<int, List<ETalents>>> cachedTalentsForClassSpecies = new Dictionary<int, Dictionary<int, List<ETalents>>>(5);
        // ClassID, RaceID, Talent List
        public static List<ETalents> TalentsForClassSpecies(PLPlayer pLPlayer, int ClassID = -1)
        {
            int RaceID = 0;
            if (ClassID != -1 && pLPlayer != null) RaceID = pLPlayer.RaceID; // 0 = Human, 1 = Sylvassi, 2 = Robot
            if (cachedTalentsForClassSpecies.ContainsKey(ClassID) && cachedTalentsForClassSpecies[ClassID].ContainsKey(RaceID))
                return cachedTalentsForClassSpecies[ClassID][RaceID];

            // Code to create class trees for new talents and for tree editing details.
            List<ETalents> list = PLGlobal.TalentsForClass(ClassID);
            foreach (TalentMod talentMod in TalentModManager.Instance.TalentTypes)
            {
                if ((talentMod.BotTalent && !pLPlayer.IsBot) || (talentMod.ClassID != ClassID && talentMod.ClassID != -1)
                    || (talentMod.Race != null && !talentMod.Race.Contains(RaceID))
                    || (talentMod.Faction != null && !talentMod.Faction.Contains(PLServer.Instance.CrewFactionID))) continue;
                list.Add((ETalents)TalentModManager.Instance.GetTalentIDFromName(talentMod.Name));
            }
            foreach(TalentTreeMod eTalents in TalentTreeModManager.Instance.TalentOverrides.Values)
            {
                if ((eTalents.BotTalent && !pLPlayer.IsBot) || (eTalents.ClassID != ClassID && eTalents.ClassID != -1)
                    || (eTalents.Race != null && !eTalents.Race.Contains(RaceID))
                    || (eTalents.Faction != null && !eTalents.Faction.Contains(PLServer.Instance.CrewFactionID)))
                {
                    if (list.Contains(eTalents.Talent)) list.Remove(eTalents.Talent);
                    continue;
                }
                if (eTalents.Disable)
                {
                    if (list.Contains(eTalents.Talent)) list.Remove(eTalents.Talent);
                    continue;
                }
                list.Add(eTalents.Talent);
            }
            //

            if (cachedTalentsForClassSpecies.ContainsKey(ClassID))
            {
                if (cachedTalentsForClassSpecies[ClassID].ContainsKey(RaceID)) cachedTalentsForClassSpecies[ClassID][RaceID] = list;
                else cachedTalentsForClassSpecies[ClassID].Add(RaceID, list);
            }
            else cachedTalentsForClassSpecies.Add(ClassID, new Dictionary<int, List<ETalents>> { { RaceID, list } });
            return list; // Temp to return all talents
        }
    }
}
