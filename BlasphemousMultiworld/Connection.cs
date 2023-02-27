using System;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using BlasphemousRandomizer.Config;
using Newtonsoft.Json.Linq;
using BlasphemousMultiworld.Structures;

namespace BlasphemousMultiworld
{
    public class Connection
    {
        private ArchipelagoSession session;
        private DeathLinkService deathLink;

        public bool connected { get; private set; }

        public string Connect(string server, string player, string password)
        {
            // Create login
            LoginResult result;
            string resultMessage;

            // Try connection
            try
            {
                session = ArchipelagoSessionFactory.CreateSession(server);
                session.Items.ItemReceived += recieveItem;
                session.Socket.SocketClosed += disconnected;
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
            GameData data = new GameData();
            ArchipelagoLocation[] locations = ((JArray)login.SlotData["locations"]).ToObject<ArchipelagoLocation[]>();
            data.gameConfig = ((JObject)login.SlotData["cfg"]).ToObject<MainConfig>();
            data.chosenEnding = int.Parse(login.SlotData["ending"].ToString());
            data.deathLinkEnabled = bool.Parse(login.SlotData["death_link"].ToString());
            data.playerName = player;

            // Set up deathlink
            deathLink = session.CreateDeathLinkService();
            deathLink.OnDeathLinkReceived += receiveDeathLink;
            setDeathLinkStatus(data.deathLinkEnabled);

            Main.Multiworld.onConnect(locations, data);
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

        // Returns a list of player names, or if unconnected then an empty list
        public string[] getPlayers()
        {
            if (connected)
            {
                string[] players = new string[session.Players.AllPlayers.Count];
                for (int i = 0; i < session.Players.AllPlayers.Count; i++)
                {
                    players[i] = session.Players.AllPlayers[i].Name;
                }
                return players;
            }
            return new string[0];
        }

        public string getServer()
        {
            if (connected)
            {
                return session.Socket.Uri.ToString();
            }
            return "";
        }

        public void setDeathLinkStatus(bool enabled)
        {
            if (connected)
            {
                if (enabled) deathLink.EnableDeathLink();
                else deathLink.DisableDeathLink();
            }
        }

        // Sends a new location check to the server
        public void sendLocation(long apLocationId)
        {
            if (connected)
            {
                session.Locations.CompleteLocationChecks(apLocationId);
            }
        }
        public void sendLocations(long[] apLocationIds)
        {
            if (connected)
            {
                session.Locations.CompleteLocationChecks(apLocationIds);
            }
        }

        // Sends goal completion to the server
        public void sendGoal()
        {
            if (connected)
            {
                StatusUpdatePacket statusUpdate = new StatusUpdatePacket();
                statusUpdate.Status = ArchipelagoClientState.ClientGoal;
                session.Socket.SendPacket(statusUpdate);
            }
        }

        // Sends player death to the server
        public void sendDeathLink()
        {
            if (connected)
            {
                deathLink.SendDeathLink(new DeathLink(Main.Multiworld.gameData.playerName));
            }
        }

        // Recieves a new item from the server
        private void recieveItem(ReceivedItemsHelper helper)
        {
            string player = session.Players.GetPlayerName(helper.PeekItem().Player);
            if (player == null || player == "") player = "Server";

            Main.Multiworld.receiveItem(helper.PeekItemName(), helper.Index, player);
            helper.DequeueItem();
        }

        // Receives a death link from the server
        private void receiveDeathLink(DeathLink link)
        {
            Main.Multiworld.receiveDeathLink(link.Source);
        }

        // Got disconnected from server
        private void disconnected(string reason)
        {
            Main.Multiworld.onDisconnect();
            connected = false;
            session = null;
        }
    }
}
