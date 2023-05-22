﻿using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using BlasphemousRandomizer;
using BlasphemousRandomizer.ItemRando;
using Framework.Managers;
using Newtonsoft.Json.Linq;

namespace BlasphemousMultiworld.AP
{
    public class APManager
    {
        private ArchipelagoSession session;
        private DeathLinkService deathLink;

        public bool Connected { get; private set; }
        public string ServerAddress => Connected ? session.Socket.Uri.ToString() : string.Empty;

        private Dictionary<string, long> apLocationIds = new Dictionary<string, long>();

        #region Connection

        public string Connect(string server, string player, string password)
        {
            // Create login
            LoginResult result;
            string resultMessage;

            // Try connection
            try
            {
                session = ArchipelagoSessionFactory.CreateSession(server);
                session.Items.ItemReceived += ReceiveItem;
                session.Socket.SocketClosed += OnDisconnect;
                result = session.TryConnectAndLogin("Blasphemous", player, ItemsHandlingFlags.RemoteItems, new Version(0, 3, 6), null, null, password);
            }
            catch (Exception e)
            {
                result = new LoginFailure(e.GetBaseException().Message);
            }

            // Connection failed
            if (!result.Successful)
            {
                Connected = false;
                LoginFailure failure = result as LoginFailure;
                resultMessage = "Multiworld connection failed: ";
                if (failure.Errors.Length > 0)
                    resultMessage += failure.Errors[0];
                else
                    resultMessage += "Reason unknown.";

                return resultMessage;
            }

            // Connection successful
            Connected = true;
            resultMessage = "Multiworld connection successful";
            LoginSuccessful login = result as LoginSuccessful;

            OnConnect(login, player);
            return resultMessage;
        }

        private void OnConnect(LoginSuccessful login, string playerName)
        {
            // Get settings from slot data
            GameSettings settings = new GameSettings();
            settings.Config = ((JObject)login.SlotData["cfg"]).ToObject<Config>();
            settings.RequiredEnding = int.Parse(login.SlotData["ending"].ToString());
            settings.DeathLinkEnabled = bool.Parse(login.SlotData["death_link"].ToString());
            settings.PlayerName = playerName;

            // Set up deathlink
            deathLink = session.CreateDeathLinkService();
            deathLink.OnDeathLinkReceived += ReceiveDeath;
            EnableDeathLink(settings.DeathLinkEnabled);

            // Get location list from slot data
            ArchipelagoLocation[] locations = ((JArray)login.SlotData["locations"]).ToObject<ArchipelagoLocation[]>();
            Dictionary<string, string> mappedItems = new Dictionary<string, string>();
            apLocationIds.Clear();

            // Process locations
            for (int i = 0; i < locations.Length; i++)
            {
                ArchipelagoLocation currentLocation = locations[i];
                // Add conversion from location id to name
                apLocationIds.Add(currentLocation.id, currentLocation.ap_id);

                // Add to new list of random items
                if (currentLocation.player_name == settings.PlayerName)
                {
                    // This is an item for this player
                    if (ItemNameExists(currentLocation.name, out string itemId))
                    {
                        mappedItems.Add(currentLocation.id, itemId);
                    }
                    else
                    {
                        Main.Multiworld.LogError("Item " + currentLocation.name + " doesn't exist!");
                        continue;
                    }
                }
                else
                {
                    // This is an item to a different game
                    mappedItems.Add(currentLocation.id, "AP"); // Will need to store index here also
                }
            }

            Main.Multiworld.OnConnect(mappedItems, settings);
        }

        public void Disconnect()
        {
            if (Connected)
            {
                session.Socket.Disconnect();
                Connected = false;
                session = null;
            }
        }

        private void OnDisconnect(string reason)
        {
            Main.Multiworld.OnDisconnect();
            Connected = false;
            session = null;
        }

        #endregion Connection

        #region Locations, items, & goal

        public void SendLocation(string location)
        {
            if (!Connected) return;

            if (apLocationIds.ContainsKey(location))
                session.Locations.CompleteLocationChecks(apLocationIds[location]);
            else
                Main.Multiworld.Log("Location " + location + " does not exist in the multiworld!");
        }

        public void SendAllLocations()
        {
            if (!Connected) return;

            List<long> checkedLocations = new List<long>();
            foreach (string location in Main.Randomizer.data.itemLocations.Keys)
            {
                if (Core.Events.GetFlag("LOCATION_" + location))
                    checkedLocations.Add(apLocationIds[location]);
            }

            Main.Multiworld.Log($"Sending all locations ({checkedLocations.Count})");
            session.Locations.CompleteLocationChecks(checkedLocations.ToArray());
        }

        private void ReceiveItem(ReceivedItemsHelper helper)
        {
            // Get information from helper
            string player = session.Players.GetPlayerName(helper.PeekItem().Player);
            if (player == null || player == string.Empty) player = "Server";
            string itemName = helper.PeekItemName();
            int itemIdx = helper.Index;
            helper.DequeueItem();

            // Process item
            if (ItemNameExists(itemName, out string itemId))
            {
                Main.Multiworld.Log("Receiving item: " + itemName);
                Main.Multiworld.QueueItem(new QueuedItem(itemId, itemIdx, player));
            }
            else
            {
                Main.Multiworld.LogDisplay("Error: " + itemName + " doesn't exist!");
            }
        }

        public void SendGoal()
        {
            if (Connected)
            {
                StatusUpdatePacket statusUpdate = new StatusUpdatePacket();
                statusUpdate.Status = ArchipelagoClientState.ClientGoal;
                session.Socket.SendPacket(statusUpdate);
            }
        }

        private bool ItemNameExists(string itemName, out string itemId)
        {
            foreach (Item item in Main.Randomizer.data.items.Values)
            {
                if (item.name == itemName)
                {
                    itemId = item.id;
                    return true;
                }
            }
            itemId = null;
            return false;
        }

        #endregion Locations, items, & goal

        #region Death link

        public void SendDeath()
        {
            if (Connected)
            {
                deathLink.SendDeathLink(new Archipelago.MultiClient.Net.BounceFeatures.DeathLink.DeathLink(Main.Multiworld.MultiworldSettings.PlayerName));
            }
        }

        private void ReceiveDeath(Archipelago.MultiClient.Net.BounceFeatures.DeathLink.DeathLink link)
        {
            Main.Multiworld.DeathLinkManager.ReceiveDeath(link.Source);
        }

        public void EnableDeathLink(bool enabled)
        {
            if (Connected)
            {
                if (enabled) deathLink.EnableDeathLink();
                else deathLink.DisableDeathLink();
            }
        }

        #endregion Death link
    }
}
