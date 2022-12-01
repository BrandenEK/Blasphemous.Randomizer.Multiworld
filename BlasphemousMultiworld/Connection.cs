using System;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Newtonsoft.Json.Linq;

namespace BlasphemousMultiworld
{
    public class Connection
    {
        private ArchipelagoSession session;
        public bool connected { get; private set; }

        public bool Connect(string server, string player)
        {
            // Create login
            LoginResult result;

            // Try connection
            try
            {
                session = ArchipelagoSessionFactory.CreateSession(server);
                session.Items.ItemReceived += recieveItem;
                result = session.TryConnectAndLogin("Blasphemous", player, Archipelago.MultiClient.Net.Enums.ItemsHandlingFlags.AllItems);
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
                string errorMessage = "Multiworld connection failed:\n";
                foreach (string error in failure.Errors)
                {
                    errorMessage += error + "\n";
                }
                Main.Randomizer.Log(errorMessage);
                return false;
            }

            // Connection successful
            connected = true;
            LoginSuccessful login = result as LoginSuccessful;
            session.Items.ItemReceived += recieveItem;
            Main.Randomizer.Log("Multiworld connection successful");

            // Retrieve new locations
            ArchipelagoLocation[] locations = ((JArray)login.SlotData["locations"]).ToObject<ArchipelagoLocation[]>();
            Main.Multiworld.onConnect(player, locations);
            return true;
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

        // Sends a new location check to the server
        public void sendLocation(long apLocationId)
        {
            if (connected)
                session.Locations.CompleteLocationChecks(apLocationId);
        }

        // Recieves a new item from the server
        private void recieveItem(ReceivedItemsHelper helper)
        {
            Main.Multiworld.recieveItem(helper.PeekItemName());
        }
    }
}
