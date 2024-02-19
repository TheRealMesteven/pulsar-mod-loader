﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static List<ETalents> Patch(PLTabMenu instance, PLPlayer pLPlayer) => TalentTrees.TalentsForClassSpecies(pLPlayer, pLPlayer.GetClassID());
    }
    internal class TalentTrees
    {
        public static Dictionary<int, Dictionary<int, List<ETalents>>> cachedTalentsForClassSpecies = new Dictionary<int, Dictionary<int, List<ETalents>>>(5);
        public static List<ETalents> TalentsForClassSpecies(PLPlayer pLPlayer, int ClassID = -1)
        {
            int RaceID = 0;
            if (ClassID != -1 && pLPlayer != null)
            {
                RaceID = pLPlayer.RaceID; // 0 = Human, 1 = Sylvassi, 2 = Robot
            }
            if (cachedTalentsForClassSpecies.ContainsKey(ClassID) && cachedTalentsForClassSpecies[ClassID].ContainsKey(RaceID))
            {
                return cachedTalentsForClassSpecies[ClassID][RaceID];
            }
            List<ETalents> list = new List<ETalents>();
            list.Add(ETalents.HEALTH_BOOST);
            list.Add(ETalents.HEALTH_BOOST_2);
            list.Add(ETalents.ANTI_RAD_INJECTION);
            list.Add(ETalents.INC_ENEMY_ATRIUM_HEAL);

            //list.Add(ETalents.PISTOL_DMG_BOOST);

            list.Add(ETalents.WPNS_RELOAD_SPEED);
            list.Add(ETalents.WPNS_RELOAD_SPEED_2);

            list.Add(ETalents.QUICK_RESPAWN);
            list.Add(ETalents.INC_STAMINA);
            list.Add(ETalents.INC_JETPACK);
            list.Add(ETalents.INC_ALLOW_ENCUMBERED_SPRINT);
            list.Add(ETalents.INC_MAX_WEIGHT);

            list.Add(ETalents.ADVANCED_OPERATOR);
            list.Add(ETalents.ITEM_UPGRADER_OPERATOR);
            list.Add(ETalents.COMPONENT_UPGRADER_OPERATOR);



            list.Add(ETalents.SCI_SCANNER_PICKUPS);
            list.Add(ETalents.SCI_SCANNER_RESEARCH_MAT);
            list.Add(ETalents.SENSOR_DISH_CERT);

            list.Add(ETalents.INC_TURRET_ZOOM);
            switch (RaceID)
            {
                case 0:     // Human 
                    
                    //list.Add((ETalents)ETalentsPlus.HEALTH_BOOST_5);
                    list.Add(ETalents.ARMOR_BOOST);
                    list.Add(ETalents.ARMOR_BOOST_2);
                    list.Add(ETalents.OXYGEN_TRAINING);
                    list.Add(ETalents.INC_HEALING_RATE); // Atrium Healing Enhancement
                    // Alchemist
                    // EXO Armour Plated
                    // EXO Armour Plated II
                    // EXO Lightweight
                    // EXO Lightweight II
                    break;
                case 1:     // Sylvassi
                    
                    break;
                case 2:     // Robot
                    
                    break;
            }
            switch (ClassID)
            {
                case 0:
                    list.Add(ETalents.CAP_CREW_SPEED_BOOST);
                    list.Add(ETalents.CAP_SCAVENGER);
                    list.Add(ETalents.CAP_ARMOR_BOOST);
                    list.Add(ETalents.CAP_DIPLOMACY);
                    list.Add(ETalents.CAP_INTIMIDATION);
                    list.Add(ETalents.CAP_SCREEN_DEFENSE);
                    list.Add(ETalents.CAP_SCREEN_SAFETY);
                    switch (RaceID)
                    {
                        case 0:     // Human 
                            break;
                        case 1:     // Sylvassi
                            break;
                        case 2:     // Robot
                            break;
                    }
                    break;
                case 1:
                    list.Add(ETalents.PIL_SHIP_TURNING);
                    list.Add(ETalents.PIL_SHIP_SPEED);
                    list.Add(ETalents.PIL_REDUCE_SYS_DAMAGE);
                    list.Add(ETalents.PIL_REDUCE_HULL_DAMAGE);
                    list.Add(ETalents.PIL_KEEN_EYES);
                    switch (RaceID)
                    {
                        case 0:     // Human 
                            break;
                        case 1:     // Sylvassi
                            break;
                        case 2:     // Robot
                            break;
                    }
                    break;
                case 2:
                    list.Add(ETalents.SCI_HEAL_NEARBY);
                    list.Add(ETalents.SCI_SENSOR_BOOST);
                    list.Add(ETalents.SCI_SENSOR_HIDE);
                    list.Add(ETalents.SCI_FREQ_AMPLIFIER);
                    list.Add(ETalents.SCI_RESEARCH_SPECIALTY);
                    list.Add(ETalents.SCI_PROBE_COOLDOWN);
                    list.Add(ETalents.SCI_PROBE_XP);
                    switch (RaceID)
                    {
                        case 0:     // Human
                            break;
                        case 1:     // Sylvassi
                            break;
                        case 2:     // Robot
                            break;
                    }
                    break;
                case 3:
                    list.Add(ETalents.WPNS_TURRET_BOOST);
                    list.Add(ETalents.WPNS_MISSILE_EXPERT);
                    list.Add(ETalents.WPNS_COOLING);
                    list.Add(ETalents.WPNS_REDUCE_PAWN_DMG);
                    list.Add(ETalents.WPNS_BOOST_CREW_TURRET_CHARGE);
                    list.Add(ETalents.WPNS_BOOST_CREW_TURRET_DAMAGE);
                    list.Add(ETalents.WPN_SCREEN_HACKER);
                    list.Add(ETalents.WPN_AMMO_BOOST);
                    list.Add(ETalents.E_TURRET_COOLING_CREW_WEAPONS);
                    switch (RaceID)
                    {
                        case 0:     // Human
                            break;
                        case 1:     // Sylvassi
                            break;
                        case 2:     // Robot
                            break;
                    }
                    break;
                case 4:
                    list.Add(ETalents.ENG_COOLANT_MIX_CUSTOM);
                    list.Add(ETalents.ENG_FIRE_REDUCTION);
                    list.Add(ETalents.ENG_REPAIR_DRONES);
                    list.Add(ETalents.ENG_WARP_CHARGE_BOOST);
                    list.Add(ETalents.ENG_AUX_POWER_BOOST);
                    list.Add(ETalents.ENG_SALVAGE);
                    list.Add(ETalents.ENG_COREPOWERBOOST);
                    list.Add(ETalents.ENG_CORECOOLINGBOOST);
                    list.Add(ETalents.E_TURRET_COOLING_CREW_ENGINEER);
                    switch (RaceID)
                    {
                        case 0:     // Human
                            break;
                        case 1:     // Sylvassi
                            break;
                        case 2:     // Robot
                            break;
                    }
                    break;
            }
            if (cachedTalentsForClassSpecies.ContainsKey(ClassID))
            {
                if (cachedTalentsForClassSpecies[ClassID].ContainsKey(RaceID))
                {
                    cachedTalentsForClassSpecies[ClassID][RaceID] = list;
                }
                else
                {
                    cachedTalentsForClassSpecies[ClassID].Add(RaceID, list);
                }
            }
            else
            {
                cachedTalentsForClassSpecies.Add(ClassID, new Dictionary<int, List<ETalents>> { { RaceID, list } });
            }
            foreach (TalentMod talentMod in TalentModManager.Instance.TalentTypes)
            {
                list.Add((ETalents)TalentModManager.Instance.GetTalentIDFromName(talentMod.Name));
            }
            return list; // Temp to return all talents
        }
    }
}
