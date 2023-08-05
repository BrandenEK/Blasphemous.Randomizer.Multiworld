using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using BlasphemousMultiworld.AP.Receivers;
using BlasphemousRandomizer;
using BlasphemousRandomizer.ItemRando;
using Framework.Managers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using ItemFlags = Archipelago.MultiClient.Net.Enums.ItemFlags;

namespace BlasphemousMultiworld.AP
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
                session.Items.ItemReceived += itemReceiver.OnReceiveItem;
                session.Socket.PacketReceived += ReceivePacket;
                session.Locations.CheckedLocationsUpdated += locationReceiver.OnReceiveLocations;
                session.Socket.SocketClosed += OnDisconnect;
                result = session.TryConnectAndLogin("Blasphemous", player, ItemsHandlingFlags.IncludeStartingInventory, new Version(0, 4, 2), null, null, password);
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
            GameSettings settings = new()
            {
                Config = ((JObject)login.SlotData["cfg"]).ToObject<Config>(),
                RequiredEnding = int.Parse(login.SlotData["ending"].ToString()),
                DeathLinkEnabled = bool.Parse(login.SlotData["death_link"].ToString()),
                PlayerName = playerName
            };

            // Set up deathlink
            deathLink = session.CreateDeathLinkService();
            deathLink.OnDeathLinkReceived += ReceiveDeath;
            EnableDeathLink(settings.DeathLinkEnabled);

            // Get door list from slot data
            Dictionary<string, string> mappedDoors = ((JObject)login.SlotData["doors"]).ToObject<Dictionary<string, string>>();

            // Get location list from slot data
            ArchipelagoLocation[] locations = ((JArray)login.SlotData["locations"]).ToObject<ArchipelagoLocation[]>();
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

        public void ProcessAllReceivers()
        {
            hintReceiver.ProcessHintQueue();
            itemReceiver.ProcessItemQueue();
            locationReceiver.ProcessLocationQueue();
            messageReceiver.ProcessMessageQueue();
        }

        public void ClearAllReceivers()
        {
            hintReceiver.ClearHintQueue();
            itemReceiver.ClearItemQueue();
            locationReceiver.ClearLocationQueue();
            messageReceiver.ClearMessageQueue();
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

        public void SendGoal()
        {
            if (Connected)
            {
                StatusUpdatePacket packet = new StatusUpdatePacket();
                packet.Status = ArchipelagoClientState.ClientGoal;
                session.Socket.SendPacket(packet);
            }
        }

        public void SendMessage(string message)
        {
            if (Connected)
            {
                SayPacket packet = new SayPacket();
                packet.Text = message;
                session.Socket.SendPacket(packet);
            }
        }

        private void ReceivePacket(ArchipelagoPacketBase packet)
        {
            if (packet.PacketType != ArchipelagoPacketType.PrintJSON)
                return;

            PrintJsonPacket jsonPacket = packet as PrintJsonPacket;
            System.Text.StringBuilder output = new();
            
            foreach (JsonMessagePart messagePart in jsonPacket.Data)
            {
                string text = messagePart.Text;
                ColorType color = ColorType.NoColor;
                switch (messagePart.Type)
                {
                    case JsonMessagePartType.ItemId:
                        {
                            if (long.TryParse(text, out long itemId))
                            {
                                if (messagePart.Flags == ItemFlags.Advancement)
                                    color = ColorType.ItemProgression;
                                else if (messagePart.Flags == ItemFlags.NeverExclude)
                                    color = ColorType.ItemUseful;
                                else if (messagePart.Flags == ItemFlags.Trap)
                                    color = ColorType.ItemTrap;
                                else
                                    color = ColorType.ItemBasic;

                                text = session.Items.GetItemName(itemId) ?? text;
                            }
                            else
                            {
                                color = ColorType.Error;
                            }
                            break;
                        }
                    case JsonMessagePartType.LocationId:
                        {
                            if (long.TryParse(text, out long locationId))
                            {
                                color = ColorType.Location;
                                text = session.Locations.GetLocationNameFromId(locationId) ?? text;
                            }
                            else
                            {
                                color = ColorType.Error;
                            }
                            break;
                        }
                    case JsonMessagePartType.PlayerId:
                        {
                            if (int.TryParse(text, out int playerId))
                            {
                                if (session.Players.GetPlayerName(playerId) == Main.Multiworld.MultiworldSettings.PlayerName)
                                    color = ColorType.PlayerSelf;
                                else
                                    color = ColorType.PlayerOther;

                                text = session.Players.GetPlayerAlias(playerId) ?? text;
                            }
                            else
                            {
                                color = ColorType.Error;
                            }
                            break;
                        }
                    case JsonMessagePartType.Color:
                        {
                            if (messagePart.Color.HasValue)
                            {
                                if (messagePart.Color.Value == JsonMessagePartColor.Red)
                                    color = ColorType.Red;
                                else if (messagePart.Color.Value == JsonMessagePartColor.Green)
                                    color = ColorType.Location;
                            }
                            else
                            {
                                color = ColorType.Error;
                            }
                            break;
                        }
                }

                if (color == ColorType.NoColor)
                {
                    //No associated color, use default white
                    output.Append(text);
                }
                else
                {
                    // Using custom color
                    output.AppendFormat("<color=#{0}>{1}</color>", colorCodes[color], text);
                }
            }

            Main.Multiworld.QueueMessage(output.ToString());
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
            return index >= 0 && index < apItems.Count ? apItems[index] : null;
        }

        public string GetPlayerNameFromSlot(int slot)
        {
            return session.Players.GetPlayerName(slot);
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

        private readonly Dictionary<ColorType, string> colorCodes = new()
        {
            { ColorType.ItemProgression, "AF99EF" },
            { ColorType.ItemUseful, "6D8BE8" },
            { ColorType.ItemTrap, "FA8072" },
            { ColorType.ItemBasic, "00EEEE" },
            { ColorType.Location, "00FF7F" },
            { ColorType.PlayerSelf, "EE00EE" },
            { ColorType.PlayerOther, "FAFAD2" },
            { ColorType.Red, "EE0000" },
            { ColorType.Error, "7F7F7F" },
        };

        private enum ColorType
        {
            ItemProgression,
            ItemUseful,
            ItemTrap,
            ItemBasic,
            Location,
            PlayerSelf,
            PlayerOther,
            Red,
            Error,
            NoColor,
        }
    }
}
