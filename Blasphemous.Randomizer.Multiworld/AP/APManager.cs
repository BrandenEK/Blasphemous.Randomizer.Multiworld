using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Blasphemous.Randomizer.Multiworld.AP.Receivers;
using Blasphemous.Randomizer.Multiworld.Models;
using Blasphemous.Randomizer.ItemRando;
using Framework.Managers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.Randomizer.Multiworld.AP
{
    public class APManager
    {
        private ArchipelagoSession session;
        private DeathLinkService deathLink;

        public bool Connected { get; private set; }
        public string ServerAddress => Connected ? session.Socket.Uri.ToString() : string.Empty;
        public int PlayerSlot => Connected ? session.ConnectionInfo.Slot : -1;

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
                result = session.TryConnectAndLogin("Blasphemous", player, ItemsHandlingFlags.AllItems, new Version(0, 5, 0), null, null, password);
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
            Config cfg = ((JObject)success.SlotData["cfg"]).ToObject<Config>();
            int ending = int.Parse(success.SlotData["ending"].ToString());
            bool dl = bool.Parse(success.SlotData["death_link"].ToString());

            Main.Multiworld.Log("Storing server settings from APManager");
            Main.Multiworld.ServerSettings = new Models.ServerSettings(cfg, ending, dl);

            // Set up deathlink
            deathLink = session.CreateDeathLinkService();
            deathLink.OnDeathLinkReceived += ReceiveDeath;
            EnableDeathLink(dl);

            // Start tracking hints
            session.DataStorage.TrackHints(hintReceiver.OnReceiveHints, true);
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
            if (!Connected)
                return;

            long id = Main.Multiworld.LocationScouter.InternalToMultiworldId(location);
            session.Locations.CompleteLocationChecks(id);
        }

        public void SendAllLocations()
        {
            if (!Connected)
                return;

            var checkedLocations = Main.Randomizer.data.itemLocations.Keys
                .Where(x => Core.Events.GetFlag("LOCATION_" + x))
                .Select(Main.Multiworld.LocationScouter.InternalToMultiworldId);

            Main.Multiworld.Log($"Sending all locations ({checkedLocations.Count()})");
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
            if (!Connected)
                return;

            session.Socket.SendPacket(new SayPacket()
            {
                Text = message
            });
        }

        public void SendHint(string item)
        {
            if (!Connected)
                return;

            session.Socket.SendPacket(new SayPacket()
            {
                Text = $"!hint {item}"
            });
        }

        public void ScoutLocation(string location)
        {
            if (!Connected)
                return;

            // If the item isnt progression belonging to another player, return
            Item item = Main.Randomizer.itemShuffler.getItemAtLocation(location);
            if (item == null || item is not MultiworldOtherItem otherItem || !otherItem.progression)
            {
                Main.Multiworld.Log("Location " + location + " does not qualify to be scouted");
                return;
            }

            // If location has already been scouted, return
            if (scoutedLocations.Contains(location))
                return;

            long id = Main.Multiworld.LocationScouter.InternalToMultiworldId(location);
            session.Locations.ScoutLocationsAsync(null, true, id);
            scoutedLocations.Add(location);
        }

        /// <summary>
        /// This method should only called right after connection, before the save is loaded in order to determine display info for all items in this world
        /// </summary>
        public void ScoutMultipleLocations(IEnumerable<long> locations, Action<Dictionary<long, ScoutedItemInfo>> callback)
        {
            session.Locations.ScoutLocationsAsync(callback, HintCreationPolicy.None, locations.ToArray());
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

        #endregion Locations, items, & goal

        #region Death link

        public void SendDeath()
        {
            if (Connected)
            {
                deathLink.SendDeathLink(new Archipelago.MultiClient.Net.BounceFeatures.DeathLink.DeathLink(Main.Multiworld.ClientSettings.Name));
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
