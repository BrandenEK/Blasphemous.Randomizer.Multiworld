using System.Collections.Generic;
using UnityEngine;
using BlasphemousRandomizer;
using BlasphemousRandomizer.ItemRando;
using BlasphemousRandomizer.Settings;
using BlasphemousMultiworld.Structures;
using Framework.Managers;
using ModdingAPI;

namespace BlasphemousMultiworld
{
    public class Multiworld : PersistentMod
    {
        public enum DeathLinkStatus { Nothing, Queued, Killing }

        // Data
        private Dictionary<string, long> apLocationIds;
        private Sprite[] multiworldImages;
        public Sprite ImageAP => multiworldImages[0];
        public Sprite ImageDeathlink => multiworldImages[1];

        // Connection
        public Connection connection { get; private set; }

        public override string PersistentID => "ID_MULTIWORLD";

        public ItemReceiver itemReceiver { get; private set; }
        public DeathLinkStatus deathlink;
        private List<QueuedItem> queuedItems;

        // Game
        public GameData MultiworldSettings { get; private set; }
        private Dictionary<string, string> multiworldMap;
        private bool gameStatus;
        private bool sentLocations;
        private int itemsReceived;

        public Multiworld(string modId, string modName, string modVersion) : base(modId, modName, modVersion)
        {
            // Set basic initialization for awake
            MultiworldSettings = new GameData();
            itemReceiver = new ItemReceiver();
        }

        protected override void Initialize()
        {
            RegisterCommand(new MultiworldCommand());

            // Create new connection
            connection = new Connection();

            // Initialize data storages
            apLocationIds = new Dictionary<string, long>();
            queuedItems = new List<QueuedItem>();

            // Load external data
            if (!FileUtil.loadDataImages("multiworld_images.png", 32, 32, 32, 0, true, out multiworldImages))
                Main.Multiworld.LogError("Error: Multiworld images could not be loaded!");
            Main.Randomizer.data.items.TryGetValue("CH", out Item cherub);
            if (cherub != null) cherub.name = "Child of Moonlight";

            Main.Multiworld.Log("Multiworld has been initialized!");
        }

        public override ModPersistentData SaveGame()
        {
            return new MultiworldPersistenceData
            {
                itemsReceived = itemsReceived
            };
        }

        public override void LoadGame(ModPersistentData data)
        {
            MultiworldPersistenceData multiworldData = (MultiworldPersistenceData)data;
            if (multiworldData != null)
            {
                itemsReceived = multiworldData.itemsReceived;
            }
        }

        public override void NewGame(bool NGPlus)
        {
            Core.Events.SetFlag("MULTIWORLD", true, false);
        }

        public override void ResetGame()
        {
            itemsReceived = 0;
        }

        protected override void LevelLoaded(string oldLevel, string newLevel)
        {
            gameStatus = newLevel != "MainMenu";
            processItems(true);
            sendAllLocations();
        }
        
        protected override void Update()
        {
            if (Input.GetKeyDown(KeyCode.Keypad9))
            {
                
            }
            else if (Input.GetKeyDown(KeyCode.Equals))
            {

            }
            
            // If you received a deathlink & are able to die
            if (MultiworldSettings.DeathLinkEnabled && deathlink == DeathLinkStatus.Queued && gameStatus && !Core.LevelManager.InsideChangeLevel && !Core.Input.HasBlocker("*"))
            {
                deathlink = DeathLinkStatus.Killing;
                Core.Logic.Penitent.KillInstanteneously();
            }
        }

        public string tryConnect(string server, string playerName, string password)
        {
            string result = connection.Connect(server, playerName, password); // Check if not in game first ?
            Main.Multiworld.Log(result);
            return result;
        }

        public void onConnect(ArchipelagoLocation[] locations, GameData serverSettings)
        {
            // Init
            apLocationIds.Clear();
            multiworldMap = new Dictionary<string, string>();
            MultiworldSettings = serverSettings;

            // Process locations
            for (int i = 0; i < locations.Length; i++)
            {
                // Add conversion from location id to name
                apLocationIds.Add(locations[i].id, locations[i].ap_id);

                // Add to new list of random items
                if (locations[i].player_name == serverSettings.PlayerName)
                {
                    // This is an item for this player
                    if (ItemNameExists(locations[i].name, out string itemId))
                    {
                        multiworldMap.Add(locations[i].id, itemId);
                    }
                    else
                    {
                        Main.Multiworld.LogError("Item " + locations[i].name + " doesn't exist!");
                        continue;
                    }
                }
                else
                {
                    // This is an item to a different game
                    multiworldMap.Add(locations[i].id, "AP");
                    //newItems.Add(locations[i].id, new ArchipelagoItem(locations[i].name, locations[i].player_name));
                }
            }

            // newItems has been filled with new shuffled items
            Main.Multiworld.Log("Game variables have been loaded from multiworld!");
            sendAllLocations();
        }

        public void onDisconnect()
        {
            Main.Multiworld.LogDisplay("Disconnected from multiworld server!");
            multiworldMap = null;
            sentLocations = false;
        }

        public Dictionary<string, string> LoadMultiworldItems() => multiworldMap;

        public void sendLocation(string location)
        {
            if (apLocationIds.ContainsKey(location))
                connection.sendLocation(apLocationIds[location]);
            else
                Main.Multiworld.Log("Location " + location + " does not exist in the multiworld!");
        }

        public void sendAllLocations()
        {
            if (sentLocations || !gameStatus || !connection.connected)
            {
                return;
            }

            // Send list of all locations already checked
            List<long> checkedLocations = new List<long>();
            foreach (string location in Main.Randomizer.data.itemLocations.Keys)
            {
                if (Core.Events.GetFlag("LOCATION_" + location))
                    checkedLocations.Add(apLocationIds[location]);
            }

            Main.Multiworld.Log($"Sending all locations ({checkedLocations.Count})");
            connection.sendLocations(checkedLocations.ToArray());
            sentLocations = true;
        }

        public void receiveItem(string itemName, int index, string player)
        {
            Main.Multiworld.Log("Receiving item: " + itemName);
            if (ItemNameExists(itemName, out string itemId))
            {
                queuedItems.Add(new QueuedItem(itemId, index, player));
                processItems(false);
            }
            else
            {
                Main.Multiworld.LogDisplay("Error: " + itemName + " doesn't exist!");
            }
        }

        // Move these into separate class
        public void sendDeathLink()
        {
            if (!MultiworldSettings.DeathLinkEnabled) return;

            Main.Multiworld.Log("Sending death link!");
            connection.sendDeathLink();
        }

        public void receiveDeathLink(string player)
        {
            if (!MultiworldSettings.DeathLinkEnabled) return;

            if (!Core.Events.GetFlag("CHERUB_RESPAWN"))
            {
                Main.Multiworld.Log("Received death link!");
                deathlink = DeathLinkStatus.Queued;
                itemReceiver.receiveItem(new QueuedItem("Death", 0, player));
            }
        }

        public bool toggleDeathLink()
        {
            bool newDeathLinkEnabled = !MultiworldSettings.DeathLinkEnabled;
            MultiworldSettings.DeathLinkEnabled = newDeathLinkEnabled;
            connection.setDeathLinkStatus(newDeathLinkEnabled);
            Main.Multiworld.Log("Setting deathlink status to " + newDeathLinkEnabled.ToString());
            return newDeathLinkEnabled;
        }

        public void processItems(bool ignoreLoadingCheck)
        {
            // Wait to process items until inside a save file and the level is loaded
            if (queuedItems.Count == 0 || !gameStatus || (!ignoreLoadingCheck && Core.LevelManager.InsideChangeLevel))
                return;

            for (int i = 0; i < queuedItems.Count; i++)
            {
                Main.Multiworld.Log($"Item '{queuedItems[i].itemId}' is at index {queuedItems[i].index} with {itemsReceived} items currently received");
                if (queuedItems[i].index > itemsReceived)
                {
                    Main.Randomizer.data.items[queuedItems[i].itemId].addToInventory();
                    itemReceiver.receiveItem(queuedItems[i]);
                    itemsReceived++;
                }
            }
            queuedItems.Clear();
        }




        public void ReachedEnding(int ending)
        {
            if (ending >= MultiworldSettings.RequiredEnding)
            {
                Main.Multiworld.Log($"Completing goal {MultiworldSettings.RequiredEnding} with ending {ending}!");
                connection.sendGoal();
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
    }
}
