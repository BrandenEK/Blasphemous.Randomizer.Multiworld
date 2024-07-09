using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Blasphemous.Randomizer.Multiworld.AP.Receivers;
using Blasphemous.Randomizer.ItemRando;
using Framework.Managers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Blasphemous.Randomizer.Multiworld.AP
{
    public class APManager
    {
        private ArchipelagoSession session;
        private DeathLinkService deathLink;

        public bool Connected { get; private set; }
        public string ServerAddress => Connected ? session.Socket.Uri.ToString() : string.Empty;
        public int PlayerSlot => Connected ? session.ConnectionInfo.Slot : -1;

        // These are cleared and refilled when connecting
        private readonly Dictionary<string, long> apLocationIds = new ();
        private readonly List<ArchipelagoItem> apItems = new ();

        // Save checked hints
        private List<string> scoutedLocations;
        public List<string> SaveScoutedLocations() => scoutedLocations;
        public void LoadScoutedLocations(List<string> locations) => scoutedLocations = locations;
        public void ClearScoutedLocations() => scoutedLocations = new List<string>();

        // AP Receivers
        private readonly HintReceiver hintReceiver = new();
        private readonly ItemReceiver itemReceiver = new();
        private readonly LocationReceiver locationReceiver = new();
        private readonly MessageReceiver messageReceiver = new();

        public ItemReceiver ItemReceiver => itemReceiver;
        public MessageReceiver MessageReceiver => messageReceiver;

        // Each receiver will be locked when processing and clearing, so you only need to lock it when receiving
        public static readonly object receiverLock = new();

        public delegate void ConnectDelegate(LoginResult login);
        public delegate void DisconnectDelegate();

        public event ConnectDelegate OnConnect;
        public event DisconnectDelegate OnDisconnect;

        public APManager()
        {
            OnConnect += OnConnected;
        }

        #region Connection

        /// <summary>
        /// Attempts to connect to the server and calls an event with the result
        /// </summary>
        public void Connect(string server, string player, string password)
        {
            LoginResult result;
            
            try
            {
                session = ArchipelagoSessionFactory.CreateSession(server);
                session.Items.ItemReceived += itemReceiver.OnReceiveItem;
                session.Socket.PacketReceived += messageReceiver.OnReceiveMessage;
                session.Locations.CheckedLocationsUpdated += locationReceiver.OnReceiveLocations;
                session.Socket.SocketClosed += OnSocketClose;
                result = session.TryConnectAndLogin("Blasphemous", player, ItemsHandlingFlags.IncludeStartingInventory, new Version(0, 5, 0), null, null, password);
            }
            catch (Exception e)
            {
                result = new LoginFailure(e.GetBaseException().Message);
            }

            Connected = result.Successful;
            OnConnect?.Invoke(result);
        }

        private void OnConnected(LoginResult login)
        {
            if (login is not LoginSuccessful success)
                return;

            // Get settings from slot data
            GameSettings settings = new()
            {
                Config = ((JObject)success.SlotData["cfg"]).ToObject<Config>(),
                RequiredEnding = int.Parse(success.SlotData["ending"].ToString()),
                DeathLinkEnabled = bool.Parse(success.SlotData["death_link"].ToString()),
                PlayerName = "Fix this later"
            };

            // Set up deathlink
            deathLink = session.CreateDeathLinkService();
            deathLink.OnDeathLinkReceived += ReceiveDeath;
            EnableDeathLink(settings.DeathLinkEnabled);

            // Get door list from slot data
            Dictionary<string, string> mappedDoors = ((JObject)success.SlotData["doors"]).ToObject<Dictionary<string, string>>();

            // Get location list from slot data
            ArchipelagoLocation[] locations = ((JArray)success.SlotData["locations"]).ToObject<ArchipelagoLocation[]>();
            Dictionary<string, string> mappedItems = new();
            apLocationIds.Clear();
            apItems.Clear();

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
                    mappedItems.Add(currentLocation.id, "AP" + apItems.Count);
                    apItems.Add(new ArchipelagoItem(currentLocation.name, currentLocation.player_name, (ArchipelagoItem.ItemType)currentLocation.type));
                }
            }

            // Start tracking hints
            session.DataStorage.TrackHints(hintReceiver.OnReceiveHints, true);

            Main.Multiworld.OnConnect(mappedItems, mappedDoors, settings);
        }

        /// <summary>
        /// Attempts to disconnect from the server and calls an event
        /// </summary>
        public void Disconnect()
        {
            if (Connected)
            {
                session.Socket.Disconnect();
                Connected = false;
                session = null;
            }
        }

        private void OnSocketClose(string reason)
        {
            Connected = false;
            session = null;

            OnDisconnect?.Invoke();
        }

        public void UpdateAllReceivers()
        {
            lock (receiverLock)
            {
                if (Main.Multiworld.InGame)
                {
                    hintReceiver.Update();
                    itemReceiver.Update();
                    locationReceiver.Update();
                }
                messageReceiver.Update(); // Doesn't need to be in game
            }
        }

        public void ClearAllReceivers()
        {
            lock (receiverLock)
            {
                hintReceiver.ClearHintQueue();
                itemReceiver.ClearItemQueue();
                locationReceiver.ClearLocationQueue();
                messageReceiver.ClearMessageQueue();
            }
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

            var checkedLocations = new List<long>();
            foreach (string location in Main.Randomizer.data.itemLocations.Keys)
            {
                if (Core.Events.GetFlag("LOCATION_" + location))
                    checkedLocations.Add(apLocationIds[location]);
            }

            Main.Multiworld.Log($"Sending all locations ({checkedLocations.Count})");
            session.Locations.CompleteLocationChecks(checkedLocations.ToArray());
        }

        public void SendGoal()
        {
            if (Connected)
            {
                var packet = new StatusUpdatePacket();
                packet.Status = ArchipelagoClientState.ClientGoal;
                session.Socket.SendPacket(packet);
            }
        }

        public void SendMessage(string message)
        {
            if (Connected)
            {
                var packet = new SayPacket();
                packet.Text = message;
                session.Socket.SendPacket(packet);
            }
        }

        public void ScoutLocation(string location)
        {
            if (!Connected) return;

            // If location doesn't exist, throw error
            if (!apLocationIds.ContainsKey(location))
            {
                Main.Multiworld.LogError("Location " + location + " does not exist in the multiworld!");
                return;
            }

            // If the item isnt progression belonging to another player, return
            Item item = Main.Randomizer.itemShuffler.getItemAtLocation(location);
            if (item == null || item.type != 200 || !((ArchipelagoItem)item).IsProgression)
            {
                Main.Multiworld.Log("Location " + location + " does not qualify to be scouted");
                return;
            }

            // If location has already been scouted, return
            if (scoutedLocations.Contains(location))
                return;

            session.Locations.ScoutLocationsAsync(null, true, apLocationIds[location]);
            scoutedLocations.Add(location);
        }

        public ArchipelagoItem GetAPItem(string apId)
        {
            int index = int.Parse(apId.Substring(2));
            return index >= 0 && index < apItems.Count ? apItems[index] : new ArchipelagoItem("Unknown Item", "Unknown Player", ArchipelagoItem.ItemType.Basic);
        }

        public string GetPlayerNameFromSlot(int slot)
        {
            return session.Players.GetPlayerName(slot); // If null it shows coming from the server
        }

        public string GetPlayerAliasFromSlot(int slot)
        {
            return session.Players.GetPlayerAlias(slot) ?? $"Player[{slot}]";
        }

        public string GetItemNameFromId(long id)
        {
            return session.Items.GetItemName(id) ?? $"Item[{id}]";
        }

        public string GetLocationNameFromId(long id)
        {
            return session.Locations.GetLocationNameFromId(id) ?? $"Location[{id}]";
        }

        public bool ItemNameExists(string itemName, out string itemId)
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

        public bool LocationIdExists(long apId, out string locationId)
        {
            foreach (KeyValuePair<string, long> locationPair in apLocationIds)
            {
                if (locationPair.Value == apId)
                {
                    locationId = locationPair.Key;
                    return true;
                }
            }

            locationId = null;
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
