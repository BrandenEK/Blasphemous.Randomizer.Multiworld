using System;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Newtonsoft.Json.Linq;
using BlasphemousRandomizer;
using BlasphemousMultiworld.Structures;

namespace BlasphemousMultiworld
{
    public class Connection
    {
        private ArchipelagoSession session;
        private DeathLinkService deathLink;

        public bool connected { get; private set; }

        public string ServerAddress => connected ? session.Socket.Uri.ToString() : string.Empty;

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
                connected = false;
                LoginFailure failure = result as LoginFailure;
                resultMessage = "Multiworld connection failed: ";
                if (failure.Errors.Length > 0)
                    resultMessage += failure.Errors[0];
                else
                    resultMessage += "Reason unknown.";

                return resultMessage;
            }

            // Connection successful
            connected = true;
            resultMessage = "Multiworld connection successful";
            LoginSuccessful login = result as LoginSuccessful;

            // Retrieve server slot data
            GameSettings settings = new GameSettings();
            ArchipelagoLocation[] locations = ((JArray)login.SlotData["locations"]).ToObject<ArchipelagoLocation[]>();
            settings.Config = ((JObject)login.SlotData["cfg"]).ToObject<Config>();
            settings.RequiredEnding = int.Parse(login.SlotData["ending"].ToString());
            settings.DeathLinkEnabled = bool.Parse(login.SlotData["death_link"].ToString());
            settings.PlayerName = player;

            // Set up deathlink
            deathLink = session.CreateDeathLinkService();
            deathLink.OnDeathLinkReceived += ReceiveDeath;
            EnableDeathLink(settings.DeathLinkEnabled);

            Main.Multiworld.onConnect(locations, settings);
            return resultMessage;
        }

        public void Disconnect()
        {
            if (connected)
            {
                session.Socket.Disconnect();
                connected = false;
                session = null;
            }
        }

        private void OnDisconnect(string reason)
        {
            Main.Multiworld.onDisconnect();
            connected = false;
            session = null;
        }

        #region Locations, items, & goal

        public void SendLocation(long apLocationId)
        {
            if (connected)
            {
                session.Locations.CompleteLocationChecks(apLocationId);
            }
        }

        public void SendMultipleLocations(long[] apLocationIds)
        {
            if (connected)
            {
                session.Locations.CompleteLocationChecks(apLocationIds);
            }
        }

        private void ReceiveItem(ReceivedItemsHelper helper)
        {
            string player = session.Players.GetPlayerName(helper.PeekItem().Player);
            if (player == null || player == string.Empty) player = "Server";

            Main.Multiworld.receiveItem(helper.PeekItemName(), helper.Index, player);
            helper.DequeueItem();
        }

        public void SendGoal()
        {
            if (connected)
            {
                StatusUpdatePacket statusUpdate = new StatusUpdatePacket();
                statusUpdate.Status = ArchipelagoClientState.ClientGoal;
                session.Socket.SendPacket(statusUpdate);
            }
        }

        #endregion Locations, items, & goal

        #region Death link

        public void SendDeath()
        {
            if (connected)
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
            if (connected)
            {
                if (enabled) deathLink.EnableDeathLink();
                else deathLink.DisableDeathLink();
            }
        }

        #endregion Death link
    }
}
