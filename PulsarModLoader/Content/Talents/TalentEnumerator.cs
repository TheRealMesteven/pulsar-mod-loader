using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PulsarModLoader.Content.Talents
{
    [HarmonyPatch(typeof(PLPlayer), "SendLateInfoTo")]
    class SendLateInfoTo
    {
        public static bool Prefix(PLPlayer __instance, PhotonPlayer inPlayer)
        {
            if (__instance.MyInventory != null)
            {
                __instance.MyInventory.OnLateJoin(inPlayer);
            }
            __instance.StartCoroutine(SafeSendLateAIInfoTo(__instance, inPlayer));
            return false;
        }
        private static IEnumerator SafeSendLateAIInfoTo(PLPlayer __instance, PhotonPlayer inPlayer)
        {
            int loops = 0;
            while (__instance.GetAIData() == null && loops < 50)
            {
                yield return new WaitForSeconds(0.5f);
                int num = loops;
                loops = num + 1;
            }
            PLPlayer playerForPhotonPlayer = PLServer.GetPlayerForPhotonPlayer(inPlayer);
            if (playerForPhotonPlayer != null && playerForPhotonPlayer.GetClassID() == 0)
            {
                __instance.StartCoroutine(SendTalentsToPhotonTargets(__instance, __instance.GetClassID(), PhotonTargets.MasterClient, true, inPlayer));
            }
            else
            {
                __instance.StartCoroutine(SendPrioritiesToPhotonTargets(__instance, __instance.GetClassID(), PhotonTargets.MasterClient, true, inPlayer));
                __instance.StartCoroutine(SendTalentsToPhotonTargets(__instance, __instance.GetClassID(), PhotonTargets.MasterClient, true, inPlayer));
            }
            yield break;
        }
        protected static FieldInfo TalentsSendTimer_MinTimeInfo = AccessTools.Field(typeof(PLPlayer), "TalentsSendTimer_MinTime");
        protected static FieldInfo EndSyncTalentsTimeInfo = AccessTools.Field(typeof(PLPlayer), "EndSyncTalentsTime");
        protected static FieldInfo CurrentlySendingTalentsToOthersInfo = AccessTools.Field(typeof(PLPlayer), "CurrentlySendingTalentsToOthers");
        private static IEnumerator SendTalentsToPhotonTargets(PLPlayer __instance, int inClassID, PhotonTargets inTargets, bool sendAll = false, PhotonPlayer inPlayer = null)
        {
            int talentID = 0;
            int maxTalentID = TalentModManager.Instance.moddedTalentMaxType;
            List<ETalents> TalentList = TalentTreeImplementation.TalentsForClassSpecies(PLServer.GetPlayerForPhotonPlayer(inPlayer), inClassID);
            while (talentID < maxTalentID && talentID < __instance.Talents.Length)
            {
                if (talentID == (int)ETalents.MAX)
                {
                    talentID++;
                    continue;
                }
                if (TalentList.Contains((ETalents)talentID) && (__instance.TalentsLocalEditTime[talentID] + (float)TalentsSendTimer_MinTimeInfo.GetValue(__instance) >= (float)EndSyncTalentsTimeInfo.GetValue(__instance) || UnityEngine.Random.Range(0, 150) == 5 || sendAll))
                {
                    if (inPlayer != null)
                    {
                        __instance.photonView.RPC("GetUpdatedTalent", inPlayer, new object[]
                        {
                        talentID,
                        __instance.Talents[talentID].GetDecrypted()
                        });
                    }
                    else
                    {
                        __instance.photonView.RPC("GetUpdatedTalent", inTargets, new object[]
                        {
                        talentID,
                        __instance.Talents[talentID].GetDecrypted()
                        });
                    }
                    float num = 0.5f;
                    if (__instance.IsBot)
                    {
                        num = 1f;
                    }
                    yield return new WaitForSeconds(num + PLNetworkManager.Instance.Instability);
                }
                talentID++;
            }
            CurrentlySendingTalentsToOthersInfo.SetValue(__instance, false);
            EndSyncTalentsTimeInfo.SetValue(__instance, Time.time);
            yield break;
        }
        protected static FieldInfo CurrentlySendingPrioritiesToOthersInfo = AccessTools.Field(typeof(PLPlayer), "CurrentlySendingPrioritiesToOthers");
        private static IEnumerator SendPrioritiesToPhotonTargets(PLPlayer __instance, int inClassID, PhotonTargets inTargets, bool sendAll = false, PhotonPlayer inPlayer = null)
        {
            if (__instance.GetAIData() != null)
            {
                List<AIPriority> list = new List<AIPriority>(__instance.GetAIData().Priorities);
                foreach (AIPriority priority in list)
                {
                    if (priority.NetDirty || sendAll || UnityEngine.Random.Range(0, 150) == 5)
                    {
                        priority.NetDirty = false;
                        if (inPlayer != null)
                        {
                            __instance.photonView.RPC("GetUpdatedPriority", inPlayer, new object[]
                            {
                            priority.PriID,
                            priority.BasePriority,
                            (int)priority.Type,
                            priority.TypeData
                            });
                        }
                        else
                        {
                            __instance.photonView.RPC("GetUpdatedPriority", inTargets, new object[]
                            {
                            priority.PriID,
                            priority.BasePriority,
                            (int)priority.Type,
                            priority.TypeData
                            });
                        }
                        yield return new WaitForSeconds(0.05f + PLNetworkManager.Instance.Instability * 0.3f);
                    }
                    List<AIPriority> list2 = new List<AIPriority>(priority.Subpriorities);
                    foreach (AIPriority aipriority in list2)
                    {
                        if (aipriority.NetDirty || sendAll || UnityEngine.Random.Range(0, 150) == 5)
                        {
                            aipriority.NetDirty = false;
                            if (inPlayer != null)
                            {
                                __instance.photonView.RPC("GetUpdatedSubPriority", inPlayer, new object[]
                                {
                                priority.PriID,
                                aipriority.PriID,
                                aipriority.BasePriority,
                                (int)aipriority.Type,
                                aipriority.TypeData
                                });
                            }
                            else
                            {
                                __instance.photonView.RPC("GetUpdatedSubPriority", inTargets, new object[]
                                {
                                priority.PriID,
                                aipriority.PriID,
                                aipriority.BasePriority,
                                (int)aipriority.Type,
                                aipriority.TypeData
                                });
                            }
                            yield return new WaitForSeconds(0.05f + PLNetworkManager.Instance.Instability * 0.3f);
                        }
                    }
                }
            }
            CurrentlySendingPrioritiesToOthersInfo.SetValue(__instance, false);
            EndSyncTalentsTimeInfo.SetValue(__instance, Time.time);
            yield break;
        }
    }
}
