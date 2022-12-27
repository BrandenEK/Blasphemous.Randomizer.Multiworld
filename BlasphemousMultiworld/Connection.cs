using System;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using BlasphemousRandomizer.Config;
using Newtonsoft.Json.Linq;
using BlasphemousMultiworld.Structures;

namespace BlasphemousMultiworld
{
    public class Connection
    {
        private ArchipelagoSession session;
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
                result = session.TryConnectAndLogin("Blasphemous", player, ItemsHandlingFlags.RemoteItems, new Version(0, 3, 6), null, null, password == "" ? null : password);
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
            ArchipelagoLocation[] locations = ((JArray)login.SlotData["locations"]).ToObject<ArchipelagoLocation[]>();
            MainConfig config = ((JObject)login.SlotData["cfg"]).ToObject<MainConfig>();
            int ending = int.Parse(login.SlotData["ending"].ToString());
            Main.Multiworld.onConnect(player, locations, config, ending);

            return resultMessage;
        }

        public void Disconnect()
        {
            if (connected)
            {
                session.Socket.Disconnect();
                connected = false;
                session = null;
                Main.Randomizer.Log("Disconnecting from multiworld server");
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

        // Recieves a new item from the server
        private void recieveItem(ReceivedItemsHelper helper)
        {
            string player = session.Players.GetPlayerName(helper.PeekItem().Player);
            Main.Multiworld.receiveItem(helper.PeekItemName(), helper.Index, player);
            helper.DequeueItem();
        }

        // Got disconnected from server
        private void disconnected(string reason)
        {
            Main.Randomizer.LogDisplay("Disconnected from multiworld server!");
            Main.Multiworld.onDisconnect();
            connected = false;
            session = null;
        }
    }
}
