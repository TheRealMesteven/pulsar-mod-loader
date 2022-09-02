﻿using ExitGames.Client.Photon;
using HarmonyLib;
using PulsarModLoader.Utilities;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Logger = PulsarModLoader.Utilities.Logger;

namespace PulsarModLoader.MPModChecks
{
    public class MPModCheckManager
    {
        public MPModCheckManager()
        {
            Instance = this;
            RefreshData();
        }

        public void RefreshData()
        {
            UpdateMyModList();
            UpdateLobbyModList();
        }

        private void UpdateLobbyModList()  //Update Photon Lobby Listing with mod list
        {
            if (PhotonNetwork.isMasterClient && PhotonNetwork.inRoom)
            {
                Room room = PhotonNetwork.room;
                Hashtable customProperties = room.CustomProperties;
                customProperties["modList"] = GetModListForLobbyListing();
                room.SetCustomProperties(customProperties);
            }
        }

        public static MPModCheckManager Instance = null;

        private MPModDataBlock[] MyModList = null;

        private Dictionary<PhotonPlayer, MPUserDataBlock> NetworkedPeersModLists = new Dictionary<PhotonPlayer, MPUserDataBlock>();

        private bool ServerHasMPMods = false;

        public MPUserDataBlock GetNetworkedPeerMods(PhotonPlayer Photonplayer)
        {
            return NetworkedPeersModLists[Photonplayer];
        }

        public void AddNetworkedPeerMods(PhotonPlayer Photonplayer, MPUserDataBlock modList)
        {
            if (NetworkedPeersModLists.ContainsKey(Photonplayer))
            {
                NetworkedPeersModLists[Photonplayer] = modList;
                return;
            }
            NetworkedPeersModLists.Add(Photonplayer, modList);
        }

        public void RemoveNetworkedPeerMods(PhotonPlayer Photonplayer)
        {
            NetworkedPeersModLists.Remove(Photonplayer);
        }

        private List<PulsarMod> GetMPModList()
        {
            List<PulsarMod> modList = new List<PulsarMod>();
            foreach (PulsarMod mod in ModManager.Instance.GetAllMods())
            {
                if (mod.MPFunctionality != (int)MPFunction.HideFromServerList)
                {
                    if (mod.MPFunctionality > (int)MPFunction.HostRequired)
                    {
                        ServerHasMPMods = true;
                    }
                    modList.Add(mod);
                }
            }
            return modList;
        }

        private void UpdateMyModList()
        {
            Logger.Info("Building MyModList");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            List<PulsarMod> UnprocessedMods = GetMPModList();
            MPModDataBlock[] ProcessedMods = new MPModDataBlock[UnprocessedMods.Count];
            using (SHA256 MyHasher = SHA256.Create())
            {
                for (int i = 0; i < UnprocessedMods.Count; i++)
                {
                    PulsarMod currentMod = UnprocessedMods[i];
                    using (FileStream MyStream = File.OpenRead(currentMod.GetType().Assembly.Location))
                    {
                        MyStream.Position = 0;
                        byte[] Hash = MyHasher.ComputeHash(MyStream);
                        ProcessedMods[i] = new MPModDataBlock(currentMod.HarmonyIdentifier(), currentMod.Name, currentMod.Version, (MPFunction)currentMod.MPFunctionality, currentMod.ModID, Hash);
                    }
                }
            }
            MyModList = ProcessedMods;
            stopwatch.Stop();
            Logger.Info("Finished Building MyModList, time elapsted: " + stopwatch.ElapsedMilliseconds.ToString());
        }

        public byte[] GetModListForLobbyListing()
        {
            MemoryStream dataStream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(dataStream))
            {
                //Datastream storage structure:
                writer.Write(Patches.GameVersion.PMLVersion);   //--Header--
                writer.Write(MyModList.Length);                 //string PMLVersion
                for (int i = 0; i < MyModList.Length; i++)      //int    modcount
                {                                               //
                    MPModDataBlock dataBlock = MyModList[i];    //--ModData--
                    writer.Write(dataBlock.ModName);            //string mod name
                    writer.Write(dataBlock.HarmonyIdentifier);  //string harmony ident
                    writer.Write(dataBlock.Version);            //string mod version
                    writer.Write((byte)dataBlock.MPFunction);   //byte   MPFunction
                    writer.Write(dataBlock.ModID);              //string ModID
                }
            }
            return dataStream.ToArray();
        }

        public static MPUserDataBlock GetModListFromLobbyListingData(byte[] byteData)
        {
            MemoryStream memoryStream = new MemoryStream(byteData);
            memoryStream.Position = 0;
            MPUserDataBlock UserData = null;
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                try
                {
                    string PMLVersion = reader.ReadString();
                    int ModCount = reader.ReadInt32();
                    MPModDataBlock[] ModList = new MPModDataBlock[ModCount];
                    for (int i = 0; i < ModCount; i++)
                    {
                        string modname = reader.ReadString();
                        string HarmonyIdent = reader.ReadString();
                        string ModVersion = reader.ReadString();
                        MPFunction MPFunction = (MPFunction)reader.ReadByte();
                        string ModID = reader.ReadString();
                        ModList[i] = new MPModDataBlock(HarmonyIdent, modname, ModVersion, MPFunction, ModID);
                    }
                    UserData = new MPUserDataBlock(PMLVersion, ModList);
                }
                catch (Exception ex)
                {
                    Logger.Info($"Failed to read mod list from lobby listing data.\n{ex.Message}");
                }
            }
            Logger.Info("Failed to read mod list from lobby listing data, returning null.");
            return UserData;
        }

        public static string GetModListAsString(MPModDataBlock[] ModDatas)
        {
            string ModList = string.Empty;
            foreach (MPModDataBlock DataBlock in ModDatas)
            {
                ModList += $"\n{DataBlock.ModName}";
            }
            return ModList;
        }

        private static MPUserDataBlock GetHostModList(RoomInfo room)
        {
            if (room.CustomProperties.ContainsKey("modList"))
            {
                return GetModListFromLobbyListingData((byte[])room.CustomProperties["modList"]);
            }
            return new MPUserDataBlock();
        }

        private static void KickClient(PhotonPlayer client)
        {
            if (SteamManager.Initialized && client.SteamID != CSteamID.Nil)
            {
                SteamUser.EndAuthSession(client.SteamID);
            }
            PhotonNetwork.CloseConnection(client);
        }

        /// <summary>
        /// overall basic description: checks if it is possible to join room based on mods installed locally and on the server
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public bool ClientClickJoinRoom(RoomInfo room)
        {
            MPUserDataBlock HostModData = GetHostModList(room);
            MPModDataBlock[] HostModList = HostModData.ModData;

            string HostModListString = GetModListAsString(HostModList);
            string LocalModListString = GetModListAsString(MyModList);
            Logger.Info($"Joining room: {room.Name} ServerPMLVersion: {HostModData.PMLVersion}\n--Hostmodlist: {HostModListString}\n--Localmodlist: {LocalModListString}");

            string missingMods = string.Empty;
            string localMPLimitedMods = string.Empty;
            string outdatedMods = string.Empty;
            int MyModListLength = MyModList.Length;
            int HostModListLength = HostModList.Length;
            for (int a = 0; a < MyModListLength; a++)
            {
                bool found = false;
                int b = 0;
                for (; b < HostModListLength; b++)
                {
                    if (HostModList[b].HarmonyIdentifier == MyModList[a].HarmonyIdentifier)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {   //didn't find mod in host list, checking if mod function mandates host installation
                    if (MyModList[a].MPFunction >= MPFunction.HostRequired)
                    {
                        localMPLimitedMods += $"\n{MyModList[a].ModName}";
                    }
                }
                else
                {   //found mod in host list, checking if mod versions match.
                    if (MyModList[a].Version != HostModList[b].Version)
                    {
                        outdatedMods += $"\nLocal: {MyModList[a].ModName} {MyModList[a].Version} Host: {HostModList[b].ModName} {HostModList[b].Version}";
                    }
                }
            }
            for (int a = 0; a < HostModListLength; a++)
            {
                bool found = false;
                for (int b = 0; b < MyModListLength; b++)
                {
                    if (HostModList[a].HarmonyIdentifier == MyModList[b].HarmonyIdentifier)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    if (HostModList[a].MPFunction == MPFunction.All)
                    {   //Host MP mod not found locally
                        missingMods += $"\n{HostModList[a].ModName}";
                    }
                }
            }
            string message = string.Empty;
            if (missingMods != string.Empty)
            {
                message += $"\n<color=yellow>YOU ARE MISSING THE FOLLOWING REQUIRED MODS</color>{missingMods}";
            }
            if (localMPLimitedMods != string.Empty)
            {
                message += $"\n<color=yellow>YOU CANNOT JOIN WITH THE FOLLOWING MODS INSTALLED</color>{localMPLimitedMods}";
            }
            if (outdatedMods != string.Empty)
            {
                message += $"\n<color=yellow>THE FOLLOWING MOD VERSIONS DO NOT MATCH</color>{outdatedMods}";
            }
            if (message != string.Empty)
            {
                PLNetworkManager.Instance.MainMenu.AddActiveMenu(new PLErrorMessageMenu($"<color=red>Failed to join crew!</color>{message}"));

                Logger.Info("Local mod list is not equal to Server mod list");
                return false;
            }
            else
            {
                Logger.Info("Modcheck passed, proceding ondwards");
                return true;
            }
        }

        public void HostOnClientJoined(PhotonPlayer Player)
        {
            MPModDataBlock[] ClientMods = null;
            bool foundplayer = false;
            if (NetworkedPeersModLists.ContainsKey(Player)) //checks if server has received mod list from client.
            {
                ClientMods = NetworkedPeersModLists[Player].ModData;
                foundplayer = true;
            }
            Logger.Info("HostOnClientJoined checking for player mods, returned " + foundplayer.ToString());


            //Checks mod list
            if (foundplayer) //If server received mod list from client
            {
                string missingMods = string.Empty;
                string clientMPLimitedMods = string.Empty;
                string outdatedMods = string.Empty;
                string incorrectHashMods = string.Empty;
                Logger.Info($"Starting Serverside Mod check");
                Logger.Info($"Checking if client is missing required mods");

                int localLength = MyModList.Length;
                int clientLength = ClientMods.Length;

                //Check mods installed locally against client
                for (int a = 0; a < localLength; a++)
                {
                    bool found = false;
                    int b = 0;
                    for (; b < clientLength; b++) 
                    {
                        if (MyModList[a].HarmonyIdentifier == ClientMods[b].HarmonyIdentifier)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        if (MyModList[a].MPFunction >= MPFunction.HostRequired) //limit these types of kicking to MPLimited mods.
                        {
                            if (MyModList[a].Version != ClientMods[b].Version)  //if mod versions don't match, add as kick reason
                            {
                                outdatedMods += $"\nLocal: {MyModList[a].ModName} {MyModList[a].Version} Client: {ClientMods[b].ModName} {ClientMods[b].Version}";
                            }
                            else if (MyModList[a].Hash != ClientMods[b].Hash)   //if mod versions match but hash doesn't, add as kick reason
                            {
                                incorrectHashMods += $"\n{ClientMods[b].ModName}";
                                missingMods += $"\n{ClientMods[b].ModName}";
                                Logger.Info("Client has bad hash for " + MyModList[a].ModName);
                            }
                        }
                    }
                    else
                    {
                        if (MyModList[a].MPFunction == MPFunction.All) //if client needs mod installed
                        {
                            missingMods += $"\n{MyModList[a].ModName}";
                        }
                    }
                }

                //Check mods installed on client against local
                for (int b = 0; b < clientLength; b++)
                {
                    bool found = false;
                    for(int a = 0; a < localLength; a++)
                    {
                        if (MyModList[a].HarmonyIdentifier == ClientMods[b].HarmonyIdentifier)
                        {
                            found = true;
                            break;
                        }
                    }
                    if(!found) //if client mod not installed locally requires host to have installed.
                    {
                        if(ClientMods[b].MPFunction >= MPFunction.HostRequired)
                        {
                            clientMPLimitedMods += $"\n{ClientMods[b].ModName}";
                        }
                    }
                }

                string message = string.Empty;
                if (missingMods != string.Empty)
                {
                    message += $"\n<color=yellow>YOU ARE MISSING THE FOLLOWING REQUIRED MODS</color>{missingMods}";
                }
                if (clientMPLimitedMods != string.Empty)
                {
                    message += $"\n<color=yellow>YOU CANNOT JOIN WITH THE FOLLOWING MODS INSTALLED</color>{clientMPLimitedMods}";
                }
                if (outdatedMods != string.Empty)
                {
                    message += $"\n<color=yellow>THE FOLLOWING MOD VERSIONS DO NOT MATCH</color>{outdatedMods}";
                }
                if (message != string.Empty)
                {
                    PLNetworkManager.Instance.MainMenu.AddActiveMenu(new PLErrorMessageMenu($"<color=red>Failed to join crew!</color>{message}"));

                    Logger.Info("Local mod list is not equal to Server mod list");
                }
                else
                {
                    Logger.Info("Modcheck passed, proceding onwards");
                }
            }
            else //client wasn't found in mod list
            {
                if (ServerHasMPMods)
                {
                    Utilities.Logger.Info("Didn't receive message or proper modlist. proceeding to kick PhotonPlayer");
                    string message = $"You have been disconnected for not having the mod loader installed";
                    ModMessageHelper.Instance.photonView.RPC("RecieveErrorMessage", Player, new object[] { message });
                    KickClient(Player);
                    return;
                }
                Utilities.Logger.Info("Didn't receive message or proper modlist, but the server doesn't have multiplayer explicit mods. Proceeding onwards");
            }
        }

        public IEnumerator ServerVerifyClient(PhotonPlayer player)
        {
            int loops = 0;
            WaitForSeconds cachedWaitTime = new WaitForSeconds(0.25f);
            while (loops < 40)
            {
                loops++;
                if (NetworkedPeersModLists.ContainsKey(player))
                {
                    break;
                }
                yield return cachedWaitTime;
            }
            HostOnClientJoined(player);
        }

        [HarmonyPatch(typeof(PLUIPlayMenu), "ActuallyJoinRoom")] //allow/disallow local client to join server.
        class JoinRoomPatch
        {
            static bool Prefix(RoomInfo room)
            {
                return Instance.ClientClickJoinRoom(room);
            }
        }

        [HarmonyPatch(typeof(PLServer), "ServerOnClientVerified")] //Starts host mod verification coroutine
        class ServerOnClientVerifiedPatch
        {
            static void Postfix(PhotonPlayer client)
            {
                PLServer.Instance.StartCoroutine(Instance.ServerVerifyClient(client));
            }
        }

        [HarmonyPatch(typeof(PLServer), "VerifyClient")] //Client sends mod info as early as possible during connection,
        class ClientJoinPatch
        {
            static void Postfix(PhotonPlayer player, PhotonMessageInfo pmi)
            {
                if (pmi.sender != null && pmi.sender.IsMasterClient && player != null)
                {
                    if (player == PhotonNetwork.player)
                    {
                        Logger.Info("Sending 'RecieveConnectionMessage' RPC");
                        ModMessageHelper.Instance.photonView.RPC("ReceiveConnectionMessage", pmi.sender, new object[]
                        {
                            MPModCheckManager.Instance.GetModListForLobbyListing()
                        });
                    }
                    player.Verified = true;
                }
            }
        }
    }
}
